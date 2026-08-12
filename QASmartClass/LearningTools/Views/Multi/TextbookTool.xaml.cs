using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using QASmartClass.LearningTools.Controls;
using QASmartTouch.Modules.InteractiveBooks.Models;
using QASmartTouch.Modules.InteractiveBooks.Services;
using QASmartClass.LearningTools.Models;

namespace QASmartClass.LearningTools.Views.Multi
{
    public partial class TextbookTool : BaseToolControl
    {
        private readonly BookService _bookService;
        private List<Book> _allBooks = new();
        private List<Book> _filteredBooks = new();
        private int _selectedGrade = 1;
        private string _selectedSeries = "all";
        private string _selectedType = "all";

        // Series color map
        private static readonly Dictionary<string, (string bg, string fg, string label)> SeriesColors = new()
        {
            ["Cánh diều"]          = ("#FFF0F0", "#D32F2F", "📕 Cánh Diều"),
            ["Kết nối tri thức"]   = ("#E3F2FD", "#1565C0", "📘 Kết Nối Tri Thức"),
            ["Chân trời sáng tạo"] = ("#E8F5E9", "#2E7D32", "📗 Chân Trời Sáng Tạo"),
        };

        private static readonly Dictionary<string, (string Bg, string Fg)> TypeColors = new()
        {
            ["SGK"] = ("#E3F2FD", "#1D4ED8"),
            ["VBT"] = ("#FFF3E0", "#E65100"),
            ["SGV"] = ("#F3E5F5", "#7B1FA2"),
            ["SBT"] = ("#E8F5E9", "#1B5E20"),
            ["CĐ"]  = ("#E0F2F1", "#00796B"),
            ["OT"]  = ("#FFEBEE", "#C62828"),
            ["VTH"] = ("#EFEBE9", "#4E342E"),
            ["STH"] = ("#ECEFF1", "#37474F"),
        };

        public TextbookTool()
        {
            InitializeComponent();
            Background = null;
            Loaded += (s, e) =>
            {
                if (Parent is Panel p) p.Background = null;
            };
            _bookService = new BookService();
            Loaded += (_, _) => { 
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                menuTextBook.Text = isVN ? "Thư viện sách" : "Textbooks Library";
                menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Practical Apps";
                
                sideMenu.SelectedIndex = 0;

                CreateGradeTabs(); 
                CreateSeriesTabs(); 
                CreateTypeTabs(); 
                LoadBooks(); 
                LoadSubjects(); 
                TouchTextPad.Attach(txtSearch, mode: "text");
                LoadPracticalApps();
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  GRADE TABS
        // ═══════════════════════════════════════════════════════════

        private void CreateGradeTabs()
        {
            gradeTabsPanel.Children.Clear();
            gradeTabsPanel.Children.Add(MakeFilterTab("Tất cả", 0, false, GradeTab_Click));
            for (int i = 1; i <= 12; i++)
                gradeTabsPanel.Children.Add(MakeFilterTab($"Lớp {i}", i, i == 1, GradeTab_Click));
        }

        private void GradeTab_Click(object sender, RoutedEventArgs e)
        {
            _selectedGrade = (int)((Button)sender).Tag;
            HighlightTabs(gradeTabsPanel, _selectedGrade);
            ApplyFilters();
        }

        // ═══════════════════════════════════════════════════════════
        //  SERIES TABS (Cánh Diều / KNTT / CTST)
        // ═══════════════════════════════════════════════════════════

        private void CreateSeriesTabs()
        {
            seriesTabsPanel.Children.Clear();
            seriesTabsPanel.Children.Add(MakeStringTab("Tất cả", "all", true, SeriesTab_Click));
            seriesTabsPanel.Children.Add(MakeStringTab("📕 Cánh Diều", "Cánh diều", false, SeriesTab_Click));
            seriesTabsPanel.Children.Add(MakeStringTab("📘 KNTT", "Kết nối tri thức", false, SeriesTab_Click));
            seriesTabsPanel.Children.Add(MakeStringTab("📗 CTST", "Chân trời sáng tạo", false, SeriesTab_Click));
        }

        private void SeriesTab_Click(object sender, RoutedEventArgs e)
        {
            _selectedSeries = (string)((Button)sender).Tag;
            HighlightStringTabs(seriesTabsPanel, _selectedSeries);
            ApplyFilters();
        }

        // ═══════════════════════════════════════════════════════════
        //  TYPE TABS (SGK / VBT / SGV / SBT / CĐ / OT / VTH)
        // ═══════════════════════════════════════════════════════════

        private void CreateTypeTabs()
        {
            typeTabsPanel.Children.Clear();
            typeTabsPanel.Children.Add(MakeStringTab("Tất cả", "all", true, TypeTab_Click));
            typeTabsPanel.Children.Add(MakeStringTab("📖 SGK", "SGK", false, TypeTab_Click));
            typeTabsPanel.Children.Add(MakeStringTab("✍️ SBT", "SBT", false, TypeTab_Click));
            typeTabsPanel.Children.Add(MakeStringTab("📝 VBT", "VBT", false, TypeTab_Click));
            typeTabsPanel.Children.Add(MakeStringTab("👨‍🏫 SGV", "SGV", false, TypeTab_Click));
            typeTabsPanel.Children.Add(MakeStringTab("✨ Chuyên đề", "CĐ", false, TypeTab_Click));
            typeTabsPanel.Children.Add(MakeStringTab("🎓 Ôn thi", "OT", false, TypeTab_Click));
            typeTabsPanel.Children.Add(MakeStringTab("🛠️ Thực hành", "VTH", false, TypeTab_Click));
        }

        private void TypeTab_Click(object sender, RoutedEventArgs e)
        {
            _selectedType = (string)((Button)sender).Tag;
            HighlightStringTabs(typeTabsPanel, _selectedType);
            ApplyFilters();
        }

        // ═══════════════════════════════════════════════════════════
        //  TAB FACTORY HELPERS
        // ═══════════════════════════════════════════════════════════

        private static Button MakeFilterTab(string label, int tag, bool selected, RoutedEventHandler handler)
        {
            var btn = new Button
            {
                Content = label, Tag = tag, Height = 38, Padding = new Thickness(14, 0, 14, 0),
                Margin = new Thickness(0, 0, 6, 0), FontSize = 14.5, Cursor = Cursors.Hand,
                FontWeight = selected ? FontWeights.Bold : FontWeights.SemiBold,
                Background = selected ? B("#4A90E2") : Brushes.White,
                Foreground = selected ? Brushes.White : B("#2F3542"),
                BorderBrush = B("#E1E8ED"), BorderThickness = new Thickness(1)
            };
            ApplyRoundTemplate(btn); btn.Click += handler; return btn;
        }

        private static Button MakeStringTab(string label, string tag, bool selected, RoutedEventHandler handler)
        {
            var btn = new Button
            {
                Content = label, Tag = tag, Height = 38, Padding = new Thickness(14, 0, 14, 0),
                Margin = new Thickness(0, 0, 6, 0), FontSize = 14.5, Cursor = Cursors.Hand,
                FontWeight = selected ? FontWeights.Bold : FontWeights.SemiBold,
                Background = selected ? B("#4A90E2") : Brushes.White,
                Foreground = selected ? Brushes.White : B("#2F3542"),
                BorderBrush = B("#E1E8ED"), BorderThickness = new Thickness(1)
            };
            ApplyRoundTemplate(btn); btn.Click += handler; return btn;
        }

        private static void ApplyRoundTemplate(Button btn)
        {
            var t = new ControlTemplate(typeof(Button));
            var f = new FrameworkElementFactory(typeof(Border));
            f.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            f.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
            f.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
            f.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            f.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            f.AppendChild(cp); t.VisualTree = f; btn.Template = t;
        }

        private static void HighlightTabs(Panel panel, int activeTag)
        {
            foreach (var child in panel.Children)
            {
                if (child is Button b)
                {
                    bool sel = (int)b.Tag == activeTag;
                    b.Background = sel ? B("#4A90E2") : Brushes.White;
                    b.Foreground = sel ? Brushes.White : B("#2F3542");
                    b.FontWeight = sel ? FontWeights.Bold : FontWeights.Normal;
                }
            }
        }

        private static void HighlightStringTabs(Panel panel, string activeTag)
        {
            foreach (var child in panel.Children)
            {
                if (child is Button b)
                {
                    bool sel = (string)b.Tag == activeTag;
                    b.Background = sel ? B("#4A90E2") : Brushes.White;
                    b.Foreground = sel ? Brushes.White : B("#2F3542");
                    b.FontWeight = sel ? FontWeights.Bold : FontWeights.Normal;
                }
            }
        }

        private static SolidColorBrush B(string hex) => new((Color)ColorConverter.ConvertFromString(hex));

        // ═══════════════════════════════════════════════════════════
        //  LOAD DATA
        // ═══════════════════════════════════════════════════════════

        private void LoadBooks()
        {
            try
            {
                _allBooks = _bookService.GetAllBooks();
                txtTotalBadge.Text = $"📚 {_allBooks.Count} sách";
                txtHeaderSub.Text = $"Lớp 1-12 • {_allBooks.Select(b => b.Subject).Distinct().Count()} môn • 3 bộ sách GDPT 2018";
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải sách: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSubjects()
        {
            try
            {
                var subjects = _bookService.GetUniqueSubjects(_allBooks);
                while (cmbSubject.Items.Count > 1) cmbSubject.Items.RemoveAt(1);
                foreach (var s in subjects) cmbSubject.Items.Add(new ComboBoxItem { Content = s });
            }
            catch { }
        }

        // ═══════════════════════════════════════════════════════════
        //  FILTER + SORT + RENDER
        // ═══════════════════════════════════════════════════════════

        private void ApplyFilters()
        {
            var list = _allBooks.AsEnumerable();

            if (_selectedGrade > 0)
                list = list.Where(b => b.Grade == _selectedGrade);

            if (_selectedSeries != "all")
                list = list.Where(b => b.Series != null && b.Series.Equals(_selectedSeries, StringComparison.OrdinalIgnoreCase));

            if (_selectedType != "all")
            {
                if (_selectedType == "VTH")
                    list = list.Where(b => b.BookType == "VTH" || b.BookType == "STH");
                else
                    list = list.Where(b => b.BookType == _selectedType);
            }

            if (cmbSubject.SelectedIndex > 0 && cmbSubject.SelectedItem is ComboBoxItem selectedItem && selectedItem.Content != null)
            {
                var subj = selectedItem.Content.ToString();
                list = list.Where(b => b.Subject == subj);
            }

            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                var q = txtSearch.Text.ToLower().Trim();
                list = list.Where(b =>
                    (b.Name ?? "").ToLower().Contains(q) ||
                    (b.Subject ?? "").ToLower().Contains(q) ||
                    (b.Publisher ?? "").ToLower().Contains(q) ||
                    (b.Series ?? "").ToLower().Contains(q));
            }

            _filteredBooks = list.ToList();
            ApplySort();
            RenderBookCards();
            UpdateStats();
        }

        private void ApplySort()
        {
            var sortTag = (cmbSort.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "name_asc";
            _filteredBooks = sortTag switch
            {
                "name_desc" => _filteredBooks.OrderByDescending(b => b.Name ?? "").ToList(),
                "subject" => _filteredBooks.OrderBy(b => b.Subject ?? "").ThenBy(b => b.Name ?? "").ToList(),
                "series" => _filteredBooks.OrderBy(b => b.Series ?? "").ThenBy(b => b.Name ?? "").ToList(),
                _ => _filteredBooks.OrderBy(b => b.Name ?? "").ToList()
            };
        }

        private void UpdateStats()
        {
            var sgkCount = _filteredBooks.Count(b => b.BookType == "SGK");
            var sbtCount = _filteredBooks.Count(b => b.BookType == "SBT");
            var vbtCount = _filteredBooks.Count(b => b.BookType == "VBT");
            var sgvCount = _filteredBooks.Count(b => b.BookType == "SGV");
            var cdCount  = _filteredBooks.Count(b => b.BookType == "CĐ");
            var otCount  = _filteredBooks.Count(b => b.BookType == "OT");
            var vthCount = _filteredBooks.Count(b => b.BookType == "VTH" || b.BookType == "STH");
            txtStats.Text = $"📖 {_filteredBooks.Count}/{_allBooks.Count} sách  •  SGK: {sgkCount}  •  SBT: {sbtCount}  •  VBT: {vbtCount}  •  SGV: {sgvCount}  •  Chuyên đề: {cdCount}  •  Ôn thi: {otCount}  •  Thực hành: {vthCount}";
        }

        // ═══════════════════════════════════════════════════════════
        //  RENDER BOOK CARDS (code-behind for full control)
        // ═══════════════════════════════════════════════════════════

        private void RenderBookCards()
        {
            bookCardsPanel.Children.Clear();
            foreach (var book in _filteredBooks)
                bookCardsPanel.Children.Add(BuildBookCard(book));
        }

        private Border BuildBookCard(Book book)
        {
            // Determine series color
            var seriesKey = SeriesColors.Keys.FirstOrDefault(k =>
                (book.Series ?? "").Equals(k, StringComparison.OrdinalIgnoreCase));
            var (seriesBg, seriesFg, seriesLabel) = seriesKey != null
                ? SeriesColors[seriesKey] : ("#F5F5F5", "#757575", $"📋 {book.Series}");
            var (typeBg, typeFg) = TypeColors.GetValueOrDefault(book.BookType, ("#4A90E2", "#FFFFFF"));

            var card = new Border
            {
                Width = 210, Background = Brushes.White, CornerRadius = new CornerRadius(10),
                BorderBrush = B("#E1E8ED"), BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 14, 14), Cursor = Cursors.Hand,
                Effect = new DropShadowEffect { BlurRadius = 8, ShadowDepth = 2, Opacity = 0.12, Color = Colors.Black, Direction = 270 }
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(260) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // ── Row 0: Cover image area ──
            var coverGrid = new Grid { ClipToBounds = true };
            coverGrid.Children.Add(new System.Windows.Shapes.Rectangle
            {
                Fill = B(seriesBg), RadiusX = 10, RadiusY = 10
            });

            // Cover image or fallback icon
            if (!string.IsNullOrEmpty(book.CoverImagePath))
            {
                try
                {
                    var bi = new System.Windows.Media.Imaging.BitmapImage();
                    bi.BeginInit();
                    bi.UriSource = new Uri(book.CoverImagePath, UriKind.Absolute);
                    bi.DecodePixelWidth = 450;
                    bi.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bi.EndInit();
                    coverGrid.Children.Add(new Image { Source = bi, Stretch = Stretch.UniformToFill });
                }
                catch { AddFallbackCover(coverGrid, book); }
            }
            else
            {
                AddFallbackCover(coverGrid, book);
            }

            // Type badge (top-left)
            var typeBadge = new Border
            {
                Background = B(typeBg), CornerRadius = new CornerRadius(4),
                HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(8, 8, 0, 0), Padding = new Thickness(8, 3, 8, 3)
            };
            typeBadge.Child = new TextBlock
            {
                Text = book.BookType, FontSize = 11, FontWeight = FontWeights.Bold,
                Foreground = B(typeFg)
            };
            coverGrid.Children.Add(typeBadge);

            // Grade circle (top-right)
            var gradeBadge = new Border
            {
                Background = B("#FF6B6B"), Width = 34, Height = 34, CornerRadius = new CornerRadius(17),
                HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 8, 8, 0)
            };
            gradeBadge.Child = new TextBlock
            {
                Text = book.Grade.ToString(), FontSize = 14, FontWeight = FontWeights.Bold,
                Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            coverGrid.Children.Add(gradeBadge);

            Grid.SetRow(coverGrid, 0);
            grid.Children.Add(coverGrid);

            // ── Row 1: Book info ──
            var infoSp = new StackPanel { Margin = new Thickness(12, 10, 12, 6) };
            infoSp.Children.Add(new TextBlock
            {
                Text = book.Name, FontSize = 15, FontWeight = FontWeights.Bold,
                Foreground = B("#2F3542"), TextWrapping = TextWrapping.Wrap, MaxHeight = 44,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            infoSp.Children.Add(new TextBlock
            {
                Text = $"{book.Subject} • Lớp {book.Grade}", FontSize = 12.5, FontWeight = FontWeights.Medium,
                Foreground = B("#95A5A6"), Margin = new Thickness(0, 4, 0, 0)
            });

            // Series badge
            var sBadge = new Border
            {
                Background = B(seriesBg), CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 3, 6, 3), Margin = new Thickness(0, 6, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            sBadge.Child = new TextBlock { Text = book.Series ?? "", FontSize = 11.5, Foreground = B(seriesFg) };
            infoSp.Children.Add(sBadge);

            Grid.SetRow(infoSp, 1);
            grid.Children.Add(infoSp);

            // ── Row 2: "Xem ngay" button ──
            var viewBtn = new Button
            {
                Content = "Xem ngay", Background = B("#4A90E2"), Foreground = Brushes.White,
                BorderThickness = new Thickness(0), Height = 38, FontSize = 14.5,
                FontWeight = FontWeights.Bold, Cursor = Cursors.Hand,
                Margin = new Thickness(12, 0, 12, 12), Tag = book
            };
            var btnTemplate = new ControlTemplate(typeof(Button));
            var btnBorder = new FrameworkElementFactory(typeof(Border));
            btnBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            btnBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            var btnContent = new FrameworkElementFactory(typeof(ContentPresenter));
            btnContent.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            btnContent.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            btnBorder.AppendChild(btnContent);
            btnTemplate.VisualTree = btnBorder;
            viewBtn.Template = btnTemplate;
            viewBtn.Click += btnViewBook_Click;
            Grid.SetRow(viewBtn, 2);
            grid.Children.Add(viewBtn);

            card.Child = grid;

            // Hover effects
            card.MouseEnter += (_, _) =>
            {
                card.BorderBrush = B(seriesFg);
                card.BorderThickness = new Thickness(2);
                card.Effect = new DropShadowEffect { BlurRadius = 20, ShadowDepth = 6, Opacity = 0.25, Color = Colors.Black, Direction = 270 };
                card.RenderTransform = new ScaleTransform(1.03, 1.03, card.Width / 2, 180);
            };
            card.MouseLeave += (_, _) =>
            {
                card.BorderBrush = B("#E1E8ED");
                card.BorderThickness = new Thickness(1);
                card.Effect = new DropShadowEffect { BlurRadius = 8, ShadowDepth = 2, Opacity = 0.12, Color = Colors.Black, Direction = 270 };
                card.RenderTransform = null;
            };

            card.MouseLeftButtonDown += (_, _) => OpenBook(book);
            return card;
        }

        private static void AddFallbackCover(Grid coverGrid, Book book)
        {
            coverGrid.Children.Add(new TextBlock
            {
                Text = book.Icon, FontSize = 64, HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center, Opacity = 0.4
            });
            coverGrid.Children.Add(new TextBlock
            {
                Text = book.Name, FontSize = 15, FontWeight = FontWeights.Bold,
                Foreground = B("#555"), TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center,
                MaxWidth = 170, HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(8, 0, 8, 25)
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  OPEN BOOK
        // ═══════════════════════════════════════════════════════════

        private void OpenBook(Book book)
        {
            try
            {
                var viewer = new QASmartTouch.Forms.Form2_20_1_BookViewer(book);
                viewer.Topmost = true;
                viewer.Owner = Window.GetWindow(this);
                viewer.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở sách: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════

        private void btnViewBook_Click(object s, RoutedEventArgs e) { OpenBook((Book)((Button)s).Tag); e.Handled = true; }
        private void txtSearch_TextChanged(object s, TextChangedEventArgs e) { if (_allBooks.Count > 0) ApplyFilters(); }
        private void cmbSubject_SelectionChanged(object s, SelectionChangedEventArgs e) { if (_allBooks.Count > 0) ApplyFilters(); }
        private void cmbSort_SelectionChanged(object s, SelectionChangedEventArgs e) { if (_allBooks.Count > 0) ApplyFilters(); }

        private void LoadPracticalApps()
        {
            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            string suffix = isVN ? "VN" : "EN";

            var items = new System.Collections.Generic.List<PracticalAppItem>
            {
                new PracticalAppItem
                {
                    Icon = "📖",
                    Title = isVN ? "Học tập & Ghi chú tương tác" : "Interactive Reading & Study",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_textbooks_1_{suffix}.png",
                    Description = isVN 
                        ? "Đọc sách giáo khoa kết hợp ghi chú, đánh dấu các công thức, định lý quan trọng trực tiếp trên trang sách giúp ghi nhớ kiến thức sâu sắc." 
                        : "Read textbooks combined with notes, highlighting key formulas and theorems directly on the page to enhance memory retention."
                },
                new PracticalAppItem
                {
                    Icon = "🎒",
                    Title = isVN ? "Tủ sách môn học cá nhân" : "Personal Subject Library",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_textbooks_2_{suffix}.png",
                    Description = isVN 
                        ? "Tự sắp xếp bộ sưu tập sách theo từng môn học (Toán, Lý, Hóa, Sinh) và theo dõi tiến độ hoàn thành các chương học một cách trực quan." 
                        : "Organize your collection of textbooks by subject (Math, Physics, Chemistry, Biology) and visually track chapter completion progress."
                },
                new PracticalAppItem
                {
                    Icon = "🔍",
                    Title = isVN ? "Tra cứu & Tìm kiếm thông minh" : "Smart Search & Index",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_textbooks_3_{suffix}.png",
                    Description = isVN 
                        ? "Bộ lọc thông minh theo cấp học (Lớp 10-12), bộ sách giáo khoa và tìm nhanh bài học theo từ khóa giúp tối ưu thời gian tra cứu học tập." 
                        : "Smart filtering by grade level (Grade 10-12), publisher series, and quick keyword search to optimize your study lookup time."
                }
            };

            try
            {
                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for TextbookTool: {Err}", ex.Message);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (rootGrid == null || sideMenu == null || viewGuide == null || viewPractice == null || viewPractical == null) return;
            
            viewGuide.Visibility = Visibility.Collapsed;
            viewPractice.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;
            
            int index = sideMenu.SelectedIndex;
            if (index == 0)
            {
                viewGuide.Visibility = Visibility.Visible;
            }
            else if (index == 1)
            {
                viewPractice.Visibility = Visibility.Visible;
            }
            else if (index == 2)
            {
                viewPractical.Visibility = Visibility.Visible;
            }
        }
    }
}