using QASmartClass.LearningTools.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.LearningTools.Helpers;
using QASmartClass.LearningTools.Models;
using Serilog;

namespace QASmartClass.LearningTools.Views
{
    public partial class LearningToolsHub : Page
    {
        private ToolCategory? _activeCategory;
        private bool _isFavoritesTabActive = false;
        private string? _activeToolId;
        private UserControl? _activeControl;
        private Window? _periodicTableWindow;
        private System.Collections.ObjectModel.ObservableCollection<StudentSubmissionItem> _submissions = new();

        public class StudentSubmissionItem
        {
            public string StudentCode { get; set; } = "";
            public string StudentName { get; set; } = "";
            public string ResultData { get; set; } = "";
            public string TimeString { get; set; } = "";

            public string DisplayText
            {
                get
                {
                    if (string.IsNullOrEmpty(ResultData)) return "";
                    int idx = ResultData.LastIndexOf(';');
                    if (idx >= 0)
                    {
                        string colorHex = ResultData.Substring(idx + 1).Trim();
                        if (colorHex.StartsWith("#") && (colorHex.Length == 4 || colorHex.Length == 5 || colorHex.Length == 7 || colorHex.Length == 9))
                        {
                            return ResultData.Substring(0, idx);
                        }
                    }
                    return ResultData;
                }
            }
        }

        public LearningToolsHub()
        {
            InitializeComponent();
            lvSubmissions.MouseDoubleClick += LvSubmissions_MouseDoubleClick;
            Loaded += (_, _) =>
            {
                BuildCategoryCards();
                BuildQuickAccess();
                RenderToolList(ToolRegistry.AllTools.ToList());
                SetSearchPlaceholder();
                TouchTextPad.Attach(txtSearch, mode: "text");

                var restoredToolId = QASmartClass.Services.LessonStateService.Instance.ActiveToolId;
                if (!string.IsNullOrEmpty(restoredToolId))
                {
                    var tool = ToolRegistry.GetById(restoredToolId);
                    if (tool != null)
                    {
                        OpenTool(tool);
                    }
                }
            };
            Unloaded += (_, _) =>
            {
                if (!string.IsNullOrEmpty(QASmartClass.Services.LessonStateService.Instance.ActiveToolFocusId))
                {
                    TeachingActionHelper.UnfocusStudents();
                }

                if (_activeControl is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                _activeControl = null;
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  QUICK ACCESS — Favorites + Recent
        // ═══════════════════════════════════════════════════════════

        private void BuildQuickAccess()
        {
            // Insert quick access section before tool list (uses toolListPanel's parent)
            var favIds = ToolUsageTracker.GetFavorites();
            var recentIds = ToolUsageTracker.GetRecent();
            if (favIds.Count == 0 && recentIds.Count == 0) return;

            // Build a horizontal strip for quick access
            var quickPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };

            if (favIds.Count > 0)
            {
                quickPanel.Children.Add(CreateQuickLabel("⭐ Yêu thích", "#E65100"));
                var favWrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
                foreach (var id in favIds)
                {
                    var tool = ToolRegistry.GetById(id);
                    if (tool == null) continue;
                    favWrap.Children.Add(CreateQuickChip(tool, "#E65100"));
                }
                quickPanel.Children.Add(favWrap);
            }

            if (recentIds.Count > 0)
            {
                quickPanel.Children.Add(CreateQuickLabel("🕐 Dùng gần đây", "#1565C0"));
                var recWrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
                foreach (var id in recentIds.Take(5))
                {
                    var tool = ToolRegistry.GetById(id);
                    if (tool == null) continue;
                    recWrap.Children.Add(CreateQuickChip(tool, "#1565C0"));
                }
                quickPanel.Children.Add(recWrap);
            }

            // Insert at top of toolListPanel
            toolListPanel.Children.Insert(0, quickPanel);
        }

        private static TextBlock CreateQuickLabel(string text, string colorHex)
        {
            var c = (Color)ColorConverter.ConvertFromString(colorHex);
            return new TextBlock
            {
                Text = text, FontSize = 13, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(c),
                Margin = new Thickness(4, 4, 0, 6)
            };
        }

        private Border CreateQuickChip(ToolDefinition tool, string colorHex)
        {
            var c = (Color)ColorConverter.ConvertFromString(colorHex);
            bool isFav = ToolUsageTracker.IsFavorite(tool.Id);
            int usage = ToolUsageTracker.GetUsageCount(tool.Id);

            var chip = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, c.R, c.G, c.B)),
                BorderBrush = new SolidColorBrush(c),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 8, 4),
                Cursor = Cursors.Hand,
                ToolTip = $"{tool.Description}\n📊 Đã dùng {usage} lần"
            };

            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new TextBlock
            {
                Text = $"{tool.Icon} {tool.Name}",
                FontSize = 12, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(c),
                VerticalAlignment = VerticalAlignment.Center
            });
            if (usage > 0)
            {
                sp.Children.Add(new TextBlock
                {
                    Text = $" ({usage})", FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromArgb(120, c.R, c.G, c.B)),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }
            chip.Child = sp;

            var capturedTool = tool;
            chip.MouseLeftButtonDown += (_, _) => OpenTool(capturedTool);
            return chip;
        }

        // ═══════════════════════════════════════════════════════════
        //  CATEGORY CARDS — 5 nhóm chính
        // ═══════════════════════════════════════════════════════════

        private void BuildCategoryCards()
        {
            categoryPanel.Children.Clear();

            var categories = new (ToolCategory Cat, string Icon, string Name, string Bg, string Fg)[]
            {
                (ToolCategory.Math,         "🔢", "Toán học",      "#E3F2FD", "#1565C0"),
                (ToolCategory.Science,      "🔬", "Khoa học",      "#E8F5E9", "#2E7D32"),
                (ToolCategory.Language,     "🗣️", "Ngôn ngữ",     "#F3E5F5", "#6A1B9A"),
                (ToolCategory.MultiSubject, "🌐", "Đa môn",        "#FFF3E0", "#E65100"),
                (ToolCategory.Thinking,     "🧠", "Tư duy & IQ",  "#FCE4EC", "#AD1457"),
                (ToolCategory.Workplace,    "💼", "Kỹ năng nghề", "#E0F2F1", "#00796B"),
                (ToolCategory.PracticalApps, "🌍", "Ứng dụng thực tế", "#E0F7FA", "#00796B"),
            };

            // "All" button
            var allCard = CreateCategoryCard("📚", "Tất cả", "#F5F5F5", "#424242",
                ToolRegistry.AllTools.Count, ToolRegistry.AllTools.Count(t => t.IsAvailable));
            allCard.MouseLeftButtonDown += (_, _) =>
            {
                _activeCategory = null;
                _isFavoritesTabActive = false;
                RenderToolList(ToolRegistry.AllTools.ToList());
                UpdateCategoryHighlight(null);
            };
            categoryPanel.Children.Add(allCard);

            // "Favorites" button
            var favList = ToolRegistry.AllTools.Where(t => ToolUsageTracker.IsFavorite(t.Id)).ToList();
            var favCard = CreateCategoryCard("★", "Yêu thích", "#FFF8E1", "#F57F17",
                favList.Count, favList.Count(t => t.IsAvailable));
            favCard.MouseLeftButtonDown += (_, _) =>
            {
                _activeCategory = null;
                _isFavoritesTabActive = true;
                RenderToolList(ToolRegistry.AllTools.Where(t => ToolUsageTracker.IsFavorite(t.Id)).ToList());
                UpdateCategoryHighlight(null);
            };
            categoryPanel.Children.Add(favCard);

            foreach (var (cat, icon, name, bg, fg) in categories)
            {
                var (total, available) = ToolRegistry.CountByCategory(cat);
                var card = CreateCategoryCard(icon, name, bg, fg, total, available);
                var capturedCat = cat;
                card.MouseLeftButtonDown += (_, _) =>
                {
                    _activeCategory = capturedCat;
                    _isFavoritesTabActive = false;
                    RenderToolList(ToolRegistry.GetByCategory(capturedCat));
                    UpdateCategoryHighlight(capturedCat);
                };
                categoryPanel.Children.Add(card);
            }
        }

        private Border CreateCategoryCard(string icon, string name, string bgHex, string fgHex, int total, int available)
        {
            var bgColor = (Color)ColorConverter.ConvertFromString(bgHex);
            var fgColor = (Color)ColorConverter.ConvertFromString(fgHex);

            var card = new Border
            {
                Background = new SolidColorBrush(bgColor),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 10, 0),
                MinWidth = 150,
                Cursor = Cursors.Hand,
                BorderBrush = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)),
                BorderThickness = new Thickness(1),
                Tag = name
            };

            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = $"{icon}  {name}",
                FontSize = 14, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(fgColor)
            });
            sp.Children.Add(new TextBlock
            {
                Text = $"{available}/{total} công cụ",
                FontSize = 10, Foreground = new SolidColorBrush(Color.FromArgb(180, fgColor.R, fgColor.G, fgColor.B)),
                Margin = new Thickness(0, 3, 0, 0)
            });

            card.Child = sp;

            card.MouseEnter += (s, _) =>
            {
                card.BorderThickness = new Thickness(2);
                card.BorderBrush = new SolidColorBrush(fgColor);
            };
            card.MouseLeave += (s, _) =>
            {
                card.BorderThickness = new Thickness(1);
                card.BorderBrush = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0));
            };

            return card;
        }

        private void UpdateCategoryHighlight(ToolCategory? active)
        {
            // Visual feedback — could be expanded
        }

        // ═══════════════════════════════════════════════════════════
        //  TOOL LIST — card grid
        // ═══════════════════════════════════════════════════════════

        private void RenderToolList(System.Collections.Generic.List<ToolDefinition> tools)
        {
            toolListPanel.Children.Clear();

            // Group by Category
            var grouped = tools
                .GroupBy(t => t.Category)
                .OrderBy(g => g.Key);

            int globalIndex = 0;
            var catMeta = new System.Collections.Generic.Dictionary<ToolCategory, (string, string, string, string, string)>
            {
                [ToolCategory.Math]         = ("🔢", "TOÁN HỌC",          "#1565C0", "#E3F2FD", "M"),
                [ToolCategory.Science]      = ("🔬", "KHOA HỌC",          "#2E7D32", "#E8F5E9", "S"),
                [ToolCategory.Language]     = ("🗣️", "NGÔN NGỮ",          "#6A1B9A", "#F3E5F5", "L"),
                [ToolCategory.MultiSubject] = ("🌐", "ĐA MÔN / TỔNG HỢP", "#E65100", "#FFF3E0", "X"),
                [ToolCategory.Thinking]     = ("🧠", "TƯ DUY & IQ",       "#AD1457", "#FCE4EC", "T"),
                [ToolCategory.Workplace]    = ("💼", "KỸ NĂNG NGHỀ NGHIỆP", "#00796B", "#E0F2F1", "W"),
                [ToolCategory.PracticalApps] = ("🌍", "ỨNG DỤNG THỰC TẾ",  "#00796B", "#E0F7FA", "P"),
            };

            foreach (var group in grouped)
            {
                var meta = catMeta.ContainsKey(group.Key)
                    ? catMeta[group.Key]
                    : ("📋", "KHÁC", "#424242", "#F5F5F5", "?");
                var fgColor = (Color)ColorConverter.ConvertFromString(meta.Item3);
                var bgColor = (Color)ColorConverter.ConvertFromString(meta.Item4);

                // ---- SECTION HEADER ----
                var sectionHeader = new Border
                {
                    Padding = new Thickness(16, 10, 16, 10),
                    Margin = new Thickness(0, 8, 12, 8),
                    CornerRadius = new CornerRadius(10),
                    Background = new SolidColorBrush(bgColor),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(60, fgColor.R, fgColor.G, fgColor.B)),
                    BorderThickness = new Thickness(0, 0, 0, 2)
                };

                var headerDock = new DockPanel();

                // Grade summary (right)
                var gradeSet = group.Select(t => t.GradeLevel).Distinct().ToList();
                var gradeSummaryTb = new TextBlock
                {
                    Text = string.Join(" \u2022 ", gradeSet),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromArgb(160, fgColor.R, fgColor.G, fgColor.B)),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                DockPanel.SetDock(gradeSummaryTb, Dock.Right);
                headerDock.Children.Add(gradeSummaryTb);

                // Title + count (left)
                var titleSp = new StackPanel { Orientation = Orientation.Horizontal };
                titleSp.Children.Add(new TextBlock
                {
                    Text = $"{meta.Item1}  {meta.Item2}",
                    FontSize = 14, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(fgColor),
                    VerticalAlignment = VerticalAlignment.Center
                });
                var countBadge = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(40, fgColor.R, fgColor.G, fgColor.B)),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(8, 2, 8, 2),
                    Margin = new Thickness(10, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                countBadge.Child = new TextBlock
                {
                    Text = $"{group.Count()} c\u00f4ng c\u1ee5",
                    FontSize = 10, Foreground = new SolidColorBrush(fgColor)
                };
                titleSp.Children.Add(countBadge);
                headerDock.Children.Add(titleSp);

                sectionHeader.Child = headerDock;

                // ---- Create section container (header + cards WrapPanel) ----
                var sectionContainer = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };
                sectionContainer.Children.Add(sectionHeader);

                var cardsWrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };

                // ---- TOOL CARDS ----
                int catIndex = 0;
                foreach (var tool in group)
                {
                    globalIndex++;
                    catIndex++;
                    var accentColor = (Color)ColorConverter.ConvertFromString(tool.ColorAccent);
                    var gradeColor = GetGradeLevelColor(tool.GradeLevel);

                    var card = new Border
                    {
                        Background = Brushes.White,
                        CornerRadius = new CornerRadius(10),
                        Padding = new Thickness(0),
                        Margin = new Thickness(0, 0, 12, 12),
                        Width = 270,
                        Cursor = tool.IsAvailable ? Cursors.Hand : Cursors.Arrow,
                        BorderBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
                        BorderThickness = new Thickness(1),
                        Opacity = tool.IsAvailable ? 1.0 : 0.55,
                        Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            BlurRadius = 8, ShadowDepth = 2, Opacity = 0.08,
                            Color = Colors.Black, Direction = 270
                        }
                    };

                    var outerGrid = new Grid();
                    outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
                    outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    // Left color stripe
                    var stripe = new Border
                    {
                        Background = new SolidColorBrush(fgColor),
                        CornerRadius = new CornerRadius(10, 0, 0, 10),
                        Width = 5
                    };
                    Grid.SetColumn(stripe, 0);
                    outerGrid.Children.Add(stripe);

                    // Main content
                    var contentGrid = new Grid { Margin = new Thickness(12, 10, 12, 10) };
                    contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var leftSp = new StackPanel();

                    // Title with serial number
                    var titlePanel = new DockPanel();
                    var serialBadge = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(30, fgColor.R, fgColor.G, fgColor.B)),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(5, 1, 5, 1),
                        Margin = new Thickness(0, 0, 6, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    serialBadge.Child = new TextBlock
                    {
                        Text = $"{meta.Item5}{catIndex:D2}",
                        FontSize = 9, FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(fgColor)
                    };
                    titlePanel.Children.Add(serialBadge);
                    titlePanel.Children.Add(new TextBlock
                    {
                        Text = $"{tool.Icon} {tool.Name}",
                        FontSize = 13, FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                        TextTrimming = TextTrimming.CharacterEllipsis
                    });
                    leftSp.Children.Add(titlePanel);

                    // Description
                    leftSp.Children.Add(new TextBlock
                    {
                        Text = tool.Description,
                        FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                        TextWrapping = TextWrapping.Wrap, MaxWidth = 200,
                        Margin = new Thickness(0, 4, 0, 6)
                    });

                    // Tags row
                    var tagPanel = new WrapPanel();
                    var gradeBadge = new Border
                    {
                        Background = new SolidColorBrush(gradeColor),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(6, 2, 6, 2),
                        Margin = new Thickness(0, 0, 4, 0)
                    };
                    gradeBadge.Child = new TextBlock
                    {
                        Text = tool.GradeLevel, FontSize = 9,
                        Foreground = Brushes.White, FontWeight = FontWeights.SemiBold
                    };
                    tagPanel.Children.Add(gradeBadge);

                    if (tool.IsInteractive)
                    {
                        var intBadge = new Border
                        {
                            Background = new SolidColorBrush(Color.FromRgb(232, 245, 233)),
                            CornerRadius = new CornerRadius(4),
                            Padding = new Thickness(6, 2, 6, 2)
                        };
                        intBadge.Child = new TextBlock
                        {
                            Text = "🎯 Interactive", FontSize = 9,
                            Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50))
                        };
                        tagPanel.Children.Add(intBadge);
                    }

                    if (!tool.IsAvailable)
                    {
                        var soonBadge = new Border
                        {
                            Background = new SolidColorBrush(Color.FromRgb(255, 243, 224)),
                            CornerRadius = new CornerRadius(4),
                            Padding = new Thickness(6, 2, 6, 2),
                            Margin = new Thickness(4, 0, 0, 0)
                        };
                        soonBadge.Child = new TextBlock
                        {
                            Text = "🔜 Sắp có", FontSize = 9,
                            Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0))
                        };
                        tagPanel.Children.Add(soonBadge);
                    }

                    leftSp.Children.Add(tagPanel);
                    Grid.SetColumn(leftSp, 0);
                    contentGrid.Children.Add(leftSp);

                    // Right: favorite star + arrow
                    if (tool.IsAvailable)
                    {
                        var rightSp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

                        // Favorite star (Redesigned with Animation)
                        bool isFav = ToolUsageTracker.IsFavorite(tool.Id);
                        var star = new TextBlock
                        {
                            Text = isFav ? "★" : "☆", 
                            FontSize = 24, 
                            Foreground = new SolidColorBrush(isFav ? Color.FromRgb(255, 179, 0) : Color.FromRgb(189, 189, 189)),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Cursor = Cursors.Hand, 
                            ToolTip = isFav ? "Bỏ yêu thích" : "Thêm yêu thích",
                            Margin = new Thickness(0, 0, 0, 4),
                            RenderTransformOrigin = new Point(0.5, 0.5),
                            RenderTransform = new ScaleTransform(1.0, 1.0)
                        };

                        // Hiệu ứng hover nhẹ khi rê chuột vào ngôi sao chưa đánh dấu
                        star.MouseEnter += (s, ev) => { if (!ToolUsageTracker.IsFavorite(tool.Id)) ((TextBlock)s).Foreground = new SolidColorBrush(Color.FromRgb(255, 213, 79)); };
                        star.MouseLeave += (s, ev) => { if (!ToolUsageTracker.IsFavorite(tool.Id)) ((TextBlock)s).Foreground = new SolidColorBrush(Color.FromRgb(189, 189, 189)); };

                        var capturedToolFav = tool;
                        star.MouseLeftButtonDown += (s, ev) =>
                        {
                            ev.Handled = true;
                            bool nowFav = ToolUsageTracker.ToggleFavorite(capturedToolFav.Id);
                            var txt = (TextBlock)s;
                            txt.Text = nowFav ? "★" : "☆";
                            txt.Foreground = new SolidColorBrush(nowFav ? Color.FromRgb(255, 179, 0) : Color.FromRgb(189, 189, 189));
                            txt.ToolTip = nowFav ? "Bỏ yêu thích" : "Thêm yêu thích";

                            // Pop Animation (Scale up and down)
                            var scale = (ScaleTransform)txt.RenderTransform;
                            var anim = new System.Windows.Media.Animation.DoubleAnimation
                            {
                                From = 1.0,
                                To = 1.5,
                                AutoReverse = true,
                                Duration = new Duration(TimeSpan.FromMilliseconds(150)),
                                EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                            };
                            scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
                            scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);

                            BuildCategoryCards(); // Cập nhật lại số lượng (count) trên các Tab
                        };
                        rightSp.Children.Add(star);

                        // Arrow
                        rightSp.Children.Add(new TextBlock
                        {
                            Text = "→", FontSize = 16,
                            Foreground = new SolidColorBrush(Color.FromArgb(80, fgColor.R, fgColor.G, fgColor.B)),
                            HorizontalAlignment = HorizontalAlignment.Center
                        });

                        Grid.SetColumn(rightSp, 1);
                        contentGrid.Children.Add(rightSp);
                    }

                    Grid.SetColumn(contentGrid, 1);
                    outerGrid.Children.Add(contentGrid);
                    card.Child = outerGrid;

                    // Click handler
                    if (tool.IsAvailable)
                    {
                        var capturedTool = tool;
                        var capturedFg = fgColor;
                        card.MouseLeftButtonDown += (_, _) => OpenTool(capturedTool);
                        card.MouseEnter += (_, _) =>
                        {
                            card.BorderBrush = new SolidColorBrush(capturedFg);
                            card.BorderThickness = new Thickness(2);
                        };
                        card.MouseLeave += (_, _) =>
                        {
                            card.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0));
                            card.BorderThickness = new Thickness(1);
                        };
                    }

                    cardsWrap.Children.Add(card);
                }

                sectionContainer.Children.Add(cardsWrap);
                toolListPanel.Children.Add(sectionContainer);
            }
        }

        /// <summary>Get badge color based on grade level</summary>
        private static Color GetGradeLevelColor(string gradeLevel)
        {
            if (gradeLevel.Contains("1-5") || gradeLevel.Contains("1-3") || gradeLevel.Contains("3-5"))
                return (Color)ColorConverter.ConvertFromString("#4CAF50");  // Ti\u1ec3u h\u1ecdc: Green
            if (gradeLevel.Contains("6-9") || gradeLevel.Contains("6-8") || gradeLevel.Contains("7-9"))
                return (Color)ColorConverter.ConvertFromString("#2196F3");  // THCS: Blue
            if (gradeLevel.Contains("10-12") || gradeLevel.Contains("11-12") || gradeLevel.Contains("10-11"))
                return (Color)ColorConverter.ConvertFromString("#7B1FA2");  // THPT: Purple
            if (gradeLevel.Contains("6-12") || gradeLevel.Contains("1-12") || gradeLevel.Contains("8-12") || gradeLevel.Contains("9-12") || gradeLevel.Contains("9-10"))
                return (Color)ColorConverter.ConvertFromString("#E65100");  // Nhi\u1ec1u c\u1ea5p: Orange
            if (gradeLevel.Contains("M\u1ecdi"))
                return (Color)ColorConverter.ConvertFromString("#546E7A");  // M\u1ecdi c\u1ea5p: Gray
            return (Color)ColorConverter.ConvertFromString("#78909C");
        }

        // ═══════════════════════════════════════════════════════════
        //  OPEN TOOL — navigate hoặc hiển thị inline
        // ═══════════════════════════════════════════════════════════

        private void OpenTool(ToolDefinition tool)
        {
            // Nếu tool link sang page khác (StemTools, PeriodicTable)
            if (!string.IsNullOrEmpty(tool.NavigateFormId))
            {
                var shell = Window.GetWindow(this) as Classroom.Views.ClassroomShell;
                shell?.NavigateTo(tool.NavigateFormId);
                return;
            }

            // Bảng Tuần Hoàn — mở Window riêng
            if (tool.Id == "periodic_table")
            {
                try
                {
                    if (_periodicTableWindow != null && _periodicTableWindow.IsLoaded)
                    {
                        if (_periodicTableWindow.WindowState == WindowState.Minimized)
                            _periodicTableWindow.WindowState = WindowState.Normal;
                        _periodicTableWindow.Activate();
                        return;
                    }

                    _periodicTableWindow = new QASmartTouch.PeriodicTable.Views.MainWindow();
                    _periodicTableWindow.Owner = Window.GetWindow(this);
                    _periodicTableWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    _periodicTableWindow.Closed += (s, e) => _periodicTableWindow = null;
                    _periodicTableWindow.Show();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Không thể mở Bảng Tuần Hoàn:\n{ex.Message}",
                        "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            // Tạo UserControl inline
            UserControl? control = CreateToolControl(tool.Id);
            if (control == null) return;

            _activeToolId = tool.Id;
            QASmartClass.Services.LessonStateService.Instance.ActiveToolId = tool.Id;
            _activeControl = control;

            // Setup submissions tracking
            _submissions.Clear();
            lvSubmissions.ItemsSource = _submissions;
            bdSubmissionSidebar.Visibility = Visibility.Collapsed;

            // Track usage
            ToolUsageTracker.RecordUsage(tool.Id);
            QASmartClass.Services.TelemetryService.Instance.Track("TOOL_OPENED", tool.Name);

            toolHost.Content = control;
            toolDetailPanel.Visibility = Visibility.Visible;

            // Build teaching action buttons
            BuildTeachingActions(tool);
        }

        /// <summary>Factory method tạo UserControl theo tool ID — dùng chung cho Hub và Broadcast</summary>
        public static UserControl? CreateToolControl(string toolId)
        {
            var control = CreateRawToolControl(toolId);
            if (control != null)
            {
                var tool = ToolRegistry.AllTools.FirstOrDefault(t => t.Id == toolId);
                if (tool != null)
                {
                    var activeBrush = new SolidColorBrush(Color.FromRgb(21, 101, 192)); // BrandPrimary fallback #1565C0
                    switch (tool.Category)
                    {
                        case ToolCategory.Math:
                            activeBrush = Application.Current.TryFindResource("CatMath") as SolidColorBrush ?? activeBrush;
                            break;
                        case ToolCategory.Science:
                            activeBrush = Application.Current.TryFindResource("CatScience") as SolidColorBrush ?? activeBrush;
                            break;
                        case ToolCategory.Language:
                            activeBrush = Application.Current.TryFindResource("CatLanguage") as SolidColorBrush ?? activeBrush;
                            break;
                        case ToolCategory.MultiSubject:
                            activeBrush = Application.Current.TryFindResource("CatMulti") as SolidColorBrush ?? activeBrush;
                            break;
                        case ToolCategory.Thinking:
                            activeBrush = Application.Current.TryFindResource("CatThinking") as SolidColorBrush ?? activeBrush;
                            break;
                        case ToolCategory.Workplace:
                            activeBrush = Application.Current.TryFindResource("CatWorkplace") as SolidColorBrush ?? activeBrush;
                            break;
                        case ToolCategory.PracticalApps:
                            activeBrush = Application.Current.TryFindResource("CatPracticalApps") as SolidColorBrush ?? activeBrush;
                            break;
                    }
                    control.Resources["TabActiveBrush"] = activeBrush;
                }
            }
            return control;
        }

        private static UserControl? CreateRawToolControl(string toolId)
        {
            return toolId switch
            {
                "stem_tools"        => new QASmartClass.LearningTools.Views.StemToolsWrapper(),
                "quickgraph"        => new QASmartClass.LearningTools.Views.StemToolsWrapper(5),
                "eqbalance"         => new QASmartClass.LearningTools.Views.StemToolsWrapper(6),
                "calculator"        => new Math.CalculatorTool(),
                "basic_math"        => new Math.BasicMathTool(),
                "multiplication"    => new Math.MultiplicationTool(),
                "trigonometry"      => new Math.TrigonometryTool(),
                "geometry"          => new Math.GeometryTool(),
                "logarithm"         => new Math.LogarithmTool(),
                "prime_numbers"     => new Math.PrimeNumberTool(),
                "number_base"       => new Math.NumberBaseTool(),
                "identities"        => new Math.IdentityTool(),
                "quadratic"         => new Math.QuadraticTool(),
                "linear_system"     => new Math.LinearSystemTool(),
                "trig_equation"     => new Math.TrigEquationTool(),
                "inequality"        => new Math.InequalityTool(),
                "sequence"          => new Math.SequenceTool(),
                "cubic"             => new Math.CubicTool(),
                "coordinate"        => new Math.CoordinateTool(),
                "combinatorics"     => new Math.CombinatoricsTool(),
                "probability"       => new Math.ProbabilityTool(),
                "derivative"        => new Math.DerivativeTool(),
                "integral"          => new Math.IntegralTool(),
                "complex_number"    => new Math.ComplexNumberTool(),
                "limit"             => new Math.LimitTool(),
                "vector"            => new Math.VectorTool(),
                "conic_section"     => new Math.ConicSectionTool(),
                "solid_geometry"    => new Math.SolidGeometryTool(),
                "fraction"          => new Math.FractionTool(),
                "math_curriculum"   => new Math.MathCurriculumTool(),
                "literature_curriculum" => new Literature.LiteratureCurriculumTool(),
                "irregular_verbs"   => new Language.IrregularVerbsTool(),
                "vocabulary"        => new Language.VocabularyTool(),
                "grammar"           => new Language.GrammarTool(),
                "ipa"               => new Language.IpaTool(),
                "ph_scale"          => new Science.PhScaleTool(),
                "density"           => new Science.DensityTool(),
                "wave_speed"        => new Science.WaveSpeedTool(),
                "boiling_freezing"  => new Science.BoilingFreezingTool(),
                "unit_converter"    => new Science.UnitConverterTool(),
                "constants"         => new Science.ConstantsTool(),
                "circuit"           => new Science.CircuitTool(),
                "lens"              => new Science.LensTool(),
                "electron_config"   => new Science.ElectronConfigTool(),
                "statistics"        => new Math.StatisticsTool(),
                "math_symbols"      => new Multi.MathSymbolsTool(),
                "planets"           => new Multi.PlanetsTool(),
                "countries"         => new Multi.CountriesTool(),
                "literature"        => new Multi.LiteratureTool(),
                "dynasties"         => new Multi.DynasTool(),
                "formulas"          => new Multi.FormulasTool(),
                "textbooks"         => new Multi.TextbookTool(),
                "notebook"          => new Multi.NotebookTool(),
                "focus_timer"       => new Multi.FocusTimerTool(),
                "brainstorm"        => new Multi.BrainstormTool(),
                "noise_monitor"     => new Multi.NoiseMonitorTool(),
                "history_timeline"  => new Multi.HistoryTimelineTool(),
                "periodic_table"    => new Multi.PeriodicTableTool(),
                "physics_sandbox"   => new Multi.PhysicsSandboxTool(),
                "mental_math"       => new Thinking.MentalMathTool(),
                "iq_quiz"           => new Thinking.IqQuizTool(),
                "sudoku"            => new Thinking.SudokuTool(),
                "memory_game"       => new Thinking.MemoryGameTool(),
                "chess_game"        => new Thinking.ChessTool(),
                "caro_game"         => new Thinking.CaroTool(),
                "genetics"          => new Science.GeneticsTool(),
                "molecular_genetics" => new Science.MolecularGeneticsTool(),
                "matrix"            => new Math.MatrixTool(),
                "mindmap"           => new Multi.MindmapTool(),
                "five_why"          => new Workplace.FiveWhyTool(),
                "swot"              => new Workplace.SwotTool(),
                "eisenhower"        => new Workplace.EisenhowerTool(),
                "fishbone"          => new Workplace.FishboneTool(),
                "pareto"            => new Workplace.ParetoTool(),
                "pdca"              => new Workplace.PdcaTool(),
                "five_s"            => new Workplace.FiveSTool(),
                "kanban"            => new Workplace.KanbanTool(),
                "kpi_okr"           => new Workplace.KpiOkrTool(),
                _ => null
            };
        }

        private void BackToHub_Click(object sender, RoutedEventArgs e)
        {
            // Unfocus nếu đang focus
            if (_activeToolId != null)
            {
                TeachingActionHelper.UnfocusSectionVisual();
                TeachingActionHelper.UnfocusStudents();
            }

            if (_activeControl is IDisposable disposable)
            {
                disposable.Dispose();
            }

            if (_activeToolId != null)
            {
                QASmartClass.Services.TelemetryService.Instance.Track("TOOL_CLOSED", _activeToolId);
            }

            _activeToolId = null;
            QASmartClass.Services.LessonStateService.Instance.ActiveToolId = null;
            _activeControl = null;
            toolHost.Content = null;
            teachingActionsPanel.Children.Clear();
            toolDetailPanel.Visibility = Visibility.Collapsed;

            // Dọn dẹp rác bộ nhớ ngay để tránh Memory Leak cho các mô hình nặng (Test Case 09)
            GC.Collect();
        }

        public void NavigateToTool(string toolId)
        {
            if (_activeToolId != null)
            {
                TeachingActionHelper.UnfocusSectionVisual();
                TeachingActionHelper.UnfocusStudents();
            }

            if (_activeControl is IDisposable disposable)
            {
                disposable.Dispose();
            }

            if (_activeToolId != null)
            {
                QASmartClass.Services.TelemetryService.Instance.Track("TOOL_CLOSED", _activeToolId);
            }

            _activeToolId = null;
            QASmartClass.Services.LessonStateService.Instance.ActiveToolId = null;
            _activeControl = null;
            toolHost.Content = null;
            teachingActionsPanel.Children.Clear();
            toolDetailPanel.Visibility = Visibility.Collapsed;

            GC.Collect();

            var tool = ToolRegistry.GetById(toolId);
            if (tool != null)
            {
                OpenTool(tool);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  TEACHING ACTIONS — Focus, Unfocus, Bảng trắng, Chiếu
        // ═══════════════════════════════════════════════════════════

        private void BuildTeachingActions(ToolDefinition tool)
        {
            teachingActionsPanel.Children.Clear();

            if (QASmartClass.LearningTools.Helpers.TeachingActionHelper.IsStudent())
            {
                teachingActionsPanel.Visibility = Visibility.Collapsed;
                return;
            }
            teachingActionsPanel.Visibility = Visibility.Visible;

            // ── Separator (tool name badge) ──
            var nameBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            nameBadge.Child = new TextBlock
            {
                Text = $"{tool.Icon} {tool.Name}",
                FontSize = 12, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66))
            };
            teachingActionsPanel.Children.Add(nameBadge);

            // ── 🎯 Focus HS ──
            var focusBtn = TeachingActionHelper.MakeActionButton(
                "🎯 Focus HS", "#E8F5E9", "#2E7D32",
                "Yêu cầu tất cả HS tập trung vào công cụ này");
            if (QASmartClass.Services.LessonStateService.Instance.ActiveToolFocusId == tool.Id)
            {
                focusBtn.Content = "✅ Đang Focus";
                focusBtn.IsEnabled = false;
                focusBtn.Background = new SolidColorBrush(Color.FromRgb(200, 230, 201));
            }
            focusBtn.Click += (s, e) =>
            {
                TeachingActionHelper.FocusStudents(tool.Id);
                focusBtn.Content = "✅ Đang Focus";
                focusBtn.IsEnabled = false;
                focusBtn.Background = new SolidColorBrush(Color.FromRgb(200, 230, 201));
                Log.Information("Focus HS on tool: {Name}", tool.Name);
            };
            teachingActionsPanel.Children.Add(focusBtn);

            // ── ❌ Unfocus ──
            var unfocusBtn = TeachingActionHelper.MakeActionButton(
                "❌ Unfocus", "#FFEBEE", "#C62828",
                "Bỏ Focus — HS tự do trở lại");
            unfocusBtn.Click += (s, e) =>
            {
                TeachingActionHelper.UnfocusSectionVisual();
                TeachingActionHelper.UnfocusStudents();
                focusBtn.Content = "🎯 Focus HS";
                focusBtn.IsEnabled = true;
                focusBtn.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#E8F5E9")!;
                Log.Information("Unfocus HS from tool: {Name}", tool.Name);
            };
            teachingActionsPanel.Children.Add(unfocusBtn);

            // ── 🖊️ Bảng trắng (ẩn cho Game tools) ──
            if (!TeachingActionHelper.IsGameTool(tool.Id))
            {
                var boardBtn = TeachingActionHelper.MakeActionButton(
                    "🖊️ Bảng trắng", "#F3E5F5", "#7B1FA2",
                    "Chụp nội dung → dán lên Bảng trắng SmartScreen");
                boardBtn.Click += async (s, e) =>
                {
                    if (_activeControl != null)
                    {
                        await TeachingActionHelper.SendToWhiteboardAsync(_activeControl, tool.Name);
                    }
                };
                teachingActionsPanel.Children.Add(boardBtn);
            }

            // ── 📺 Chiếu ──
            var screenBtn = TeachingActionHelper.MakeActionButton(
                "📺 Chiếu", "#E3F2FD", "#1565C0",
                "Phát nội dung lên SmartScreen cho cả lớp xem");
            screenBtn.Click += (s, e) =>
            {
                if (_activeControl != null)
                {
                    var ok = TeachingActionHelper.BroadcastToScreen(_activeControl, tool.Name);
                    if (ok)
                    {
                        screenBtn.Content = "📺 Đang chiếu";
                        screenBtn.Background = new SolidColorBrush(Color.FromRgb(187, 222, 251));
                    }
                }
            };
            teachingActionsPanel.Children.Add(screenBtn);
        }

        // ═══════════════════════════════════════════════════════════
        //  SEARCH
        // ═══════════════════════════════════════════════════════════

        private void Search_Changed(object sender, TextChangedEventArgs e)
        {
            var query = txtSearch.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(query) || query == "Tìm kiếm công cụ...")
            {
                if (_isFavoritesTabActive)
                    RenderToolList(ToolRegistry.AllTools.Where(t => ToolUsageTracker.IsFavorite(t.Id)).ToList());
                else if (_activeCategory.HasValue)
                    RenderToolList(ToolRegistry.GetByCategory(_activeCategory.Value));
                else
                    RenderToolList(ToolRegistry.AllTools.ToList());
                return;
            }

            RenderToolList(ToolRegistry.Search(query));
        }

        private void SetSearchPlaceholder()
        {
            if (string.IsNullOrEmpty(txtSearch.Text))
            {
                txtSearch.Text = "Tìm kiếm công cụ...";
                txtSearch.Foreground = Brushes.Gray;
            }
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == "Tìm kiếm công cụ...")
            {
                txtSearch.Text = "";
                txtSearch.Foreground = Brushes.Black;
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
                SetSearchPlaceholder();
        }

        public void HandleStudentSubmission(string toolId, string studentCode, string studentName, string resultData)
        {
            if (_activeToolId != toolId) return;

            string cleanName = Helpers.PathHelper.SanitizeInput(studentName, 50);
            string cleanData = Helpers.PathHelper.SanitizeInput(resultData, 300);

            Dispatcher.Invoke(() =>
            {
                bdSubmissionSidebar.Visibility = Visibility.Visible;

                var existing = _submissions.FirstOrDefault(s => s.StudentCode == studentCode);
                if (existing != null)
                {
                    existing.StudentName = cleanName;
                    existing.ResultData = cleanData;
                    existing.TimeString = DateTime.Now.ToString("HH:mm:ss");
                    lvSubmissions.Items.Refresh();
                }
                else
                {
                    _submissions.Add(new StudentSubmissionItem
                    {
                        StudentCode = studentCode,
                        StudentName = cleanName,
                        ResultData = cleanData,
                        TimeString = DateTime.Now.ToString("HH:mm:ss")
                    });
                }
            });
        }

        private void HideSubmissions_Click(object sender, RoutedEventArgs e)
        {
            bdSubmissionSidebar.Visibility = Visibility.Collapsed;
        }

        private void LvSubmissions_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lvSubmissions.SelectedItem is StudentSubmissionItem item)
            {
                PinSubmissionToBoard(item);
            }
        }

        private void PinSubmissionToBoard(StudentSubmissionItem item)
        {
            if (_activeControl is Multi.BrainstormTool brainstorm)
            {
                string text = item.ResultData;
                Color color = Color.FromRgb(255, 249, 196); // Vàng mặc định

                int lastSemi = item.ResultData.LastIndexOf(';');
                if (lastSemi >= 0)
                {
                    string colorHex = item.ResultData.Substring(lastSemi + 1).Trim();
                    if (colorHex.StartsWith("#") && (colorHex.Length == 4 || colorHex.Length == 5 || colorHex.Length == 7 || colorHex.Length == 9))
                    {
                        text = item.ResultData.Substring(0, lastSemi);
                        try
                        {
                            color = (Color)ColorConverter.ConvertFromString(colorHex);
                        }
                        catch { }
                    }
                }

                // Định dạng nội dung kèm tên người đóng góp
                string noteContent = $"{text}\n\n✍️ Đóng góp từ: {item.StudentName}";

                // Tạo vị trí ngẫu nhiên
                var rng = new Random();
                double x = rng.Next(100, global::System.Math.Max(200, (int)brainstorm.BoardCanvas.ActualWidth - 300));
                double y = rng.Next(100, global::System.Math.Max(200, (int)brainstorm.BoardCanvas.ActualHeight - 300));

                brainstorm.AddNoteAt(noteContent, color, x, y, focusTextBox: false);

                // Auto-remove pinned submission
                _submissions.Remove(item);
                if (_submissions.Count == 0)
                {
                    bdSubmissionSidebar.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void RejectSubmission_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is StudentSubmissionItem item)
            {
                _submissions.Remove(item);
                if (_submissions.Count == 0)
                {
                    bdSubmissionSidebar.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}

