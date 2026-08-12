using QASmartClass.LearningTools.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using QASmartClass.LearningTools;
using QASmartClass.LearningTools.Controls;

namespace QASmartClass.LearningTools.Views.Science
{
    public partial class ConstantsTool : BaseToolControl
    {
        private string _activeCategory = "Tất cả";

        public ConstantsTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextLookup != null) menuTextLookup.Text = isVN ? "Bảng tra cứu" : "Lookup Table";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                BuildCategoryTabs();
                RenderConstants("Tất cả", null);
                TouchTextPad.Attach(txtSearch, mode: "text");
                LoadPracticalApps();
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  DATA — 32 hằng số, 5 nhóm với CopyValue & Màu đồng bộ
        // ═══════════════════════════════════════════════════════════

        private static readonly List<(string Symbol, string Name, string Value, string CopyValue, string Unit, string Category, string Color, string Desc)> AllConstants = new()
        {
            // ── Vật lý cơ bản (Màu nền lam nhạt: #E3F2FD) ──
            ("c",     "Tốc độ ánh sáng",                 "299 792 458",       "299792458",       "m/s",        "Vật lý",  "#E3F2FD",
             "Tốc độ truyền ánh sáng trong chân không. Albert Einstein: E = mc²"),
            ("h",     "Hằng số Planck",                   "6.626 × 10⁻³⁴",    "6.626e-34",       "J·s",        "Vật lý",  "#E3F2FD",
             "Liên hệ năng lượng photon với tần số: E = hf"),
            ("ℏ",     "Hằng số Planck rút gọn",           "1.055 × 10⁻³⁴",    "1.055e-34",       "J·s",        "Vật lý",  "#E3F2FD",
             "ℏ = h/2π, dùng trong cơ học lượng tử"),
            ("G",     "Hằng số hấp dẫn",                  "6.674 × 10⁻¹¹",    "6.674e-11",       "N·m²/kg²",   "Vật lý",  "#E3F2FD",
             "Định luật vạn vật hấp dẫn Newton: F = GMm/r²"),
            ("g",     "Gia tốc trọng trường",             "9.80665",           "9.80665",         "m/s²",       "Vật lý",  "#E3F2FD",
             "Gia tốc rơi tự do trên bề mặt Trái Đất. Thường lấy bằng 9.8 hoặc 10 m/s² trong tính toán phổ thông."),
            ("μ₀",    "Độ từ thẩm của chân không",        "4π × 10⁻⁷",        "1.256637e-6",     "T·m/A",      "Vật lý",  "#E3F2FD",
             "Hằng số từ của chân không"),
            ("ε₀",    "Độ điện thẩm của chân không",      "8.854 × 10⁻¹²",    "8.854e-12",       "F/m",        "Vật lý",  "#E3F2FD",
             "Hằng số điện của chân không"),
            ("σ",     "Hằng số Stefan-Boltzmann",          "5.670 × 10⁻⁸",    "5.670e-8",        "W/(m²·K⁴)", "Vật lý",  "#E3F2FD",
             "Bức xạ nhiệt vật đen tuyệt đối: P = σAT⁴"),

            // ── Hóa học (Màu nền lục nhạt: #E8F5E9) ──
            ("kB",    "Hằng số Boltzmann",                "1.381 × 10⁻²³",    "1.381e-23",       "J/K",        "Hóa học", "#E8F5E9",
             "Liên hệ nhiệt độ với năng lượng phân tử"),
            ("e",     "Điện tích electron",                "1.602 × 10⁻¹⁹",   "1.602e-19",       "C",          "Hóa học", "#E8F5E9",
             "Điện tích cơ bản (độ lớn), đơn vị điện tích nhỏ nhất"),
            ("NA",    "Số Avogadro",                       "6.022 × 10²³",     "6.022e23",        "mol⁻¹",     "Hóa học", "#E8F5E9",
             "Số hạt đơn vị trong 1 mol chất"),
            ("R",     "Hằng số khí lý tưởng",               "8.31446",           "8.31446",         "J/(mol·K)", "Hóa học", "#E8F5E9",
             "Hằng số khí trong phương trình trạng thái: PV = nRT"),
            ("F",     "Hằng số Faraday",                   "96 485",            "96485",           "C/mol",     "Hóa học", "#E8F5E9",
             "Điện lượng của 1 mol điện tích. Thường lấy bằng 96.500 C/mol trong bài tập điện phân."),
            ("u",     "Đơn vị khối lượng nguyên tử",       "1.661 × 10⁻²⁷",    "1.661e-27",       "kg",        "Hóa học", "#E8F5E9",
             "Định nghĩa bằng 1/12 khối lượng nguyên tử carbon-12"),

            // ── Hạt nhân & Lượng tử (Màu nền tím nhạt: #F3E5F5) ──
            ("me",    "Khối lượng electron",               "9.109 × 10⁻³¹",    "9.109e-31",       "kg",         "Hạt nhân", "#F3E5F5",
             "Khối lượng nghỉ của electron (≈ 0.00055 u)"),
            ("mp",    "Khối lượng proton",                 "1.673 × 10⁻²⁷",    "1.673e-27",       "kg",         "Hạt nhân", "#F3E5F5",
             "Khối lượng nghỉ của proton (≈ 1.00728 u)"),
            ("mn",    "Khối lượng neutron",                "1.675 × 10⁻²⁷",    "1.675e-27",       "kg",         "Hạt nhân", "#F3E5F5",
             "Khối lượng nghỉ của neutron (≈ 1.00866 u)"),
            ("α",     "Hằng số cấu trúc tinh tế",         "1/137.036",         "0.00729735",      "—",          "Hạt nhân", "#F3E5F5",
             "Quy định cường độ của tương tác điện từ"),
            ("a₀",    "Bán kính Bohr",                     "5.292 × 10⁻¹¹",    "5.292e-11",       "m",          "Hạt nhân", "#F3E5F5",
             "Bán kính quỹ đạo nhỏ nhất của electron trong nguyên tử hydro"),
            ("Ry",    "Hằng số Rydberg",                   "1.097 × 10⁷",      "1.097e7",         "m⁻¹",       "Hạt nhân", "#F3E5F5",
             "Dùng trong công thức tính phổ vạch phát xạ nguyên tử hydro"),

            // ── Thiên văn (Màu nền cam nhạt: #FFF3E0) ──
            ("AU",    "Đơn vị thiên văn",                  "1.496 × 10¹¹",     "1.496e11",        "m",          "Thiên văn", "#FFF3E0",
             "Khoảng cách trung bình giữa Trái Đất và Mặt Trời"),
            ("ly",    "Năm ánh sáng",                      "9.461 × 10¹⁵",     "9.461e15",        "m",          "Thiên văn", "#FFF3E0",
             "Quãng đường ánh sáng truyền đi trong chân không trong 1 năm"),
            ("pc",    "Parsec",                             "3.086 × 10¹⁶",    "3.086e16",        "m",          "Thiên văn", "#FFF3E0",
             "1 parsec ≈ 3.26 năm ánh sáng"),
            ("M☉",   "Khối lượng Mặt Trời",               "1.989 × 10³⁰",     "1.989e30",        "kg",         "Thiên văn", "#FFF3E0",
             "Khối lượng của Mặt Trời, đơn vị so sánh trong thiên văn"),
            ("R⊕",   "Bán kính Trái Đất",                 "6.371 × 10⁶",      "6.371e6",         "m",          "Thiên văn", "#FFF3E0",
             "Bán kính trung bình của Trái Đất"),

            // ── Toán học (Màu nền hồng nhạt: #FFEBEE) ──
            ("π",     "Số Pi",                             "3.14159265358979",  "3.14159265358979", "—",          "Toán học", "#FFEBEE",
             "Tỷ số giữa chu vi và đường kính của một đường tròn"),
            ("e",     "Số Euler",                          "2.71828182845905",  "2.71828182845905", "—",          "Toán học", "#FFEBEE",
             "Cơ số của logarit tự nhiên: lim_{n→∞} (1 + 1/n)ⁿ"),
            ("φ",     "Tỷ lệ vàng",                       "1.61803398874989",  "1.61803398874989", "—",          "Toán học", "#FFEBEE",
             "φ = (1 + √5)/2, xuất hiện nhiều trong tự nhiên và mỹ thuật"),
            ("√2",    "Căn bậc hai của 2",                 "1.41421356237310",  "1.41421356237310", "—",          "Toán học", "#FFEBEE",
             "Độ dài đường chéo hình vuông có cạnh bằng 1"),
            ("√3",    "Căn bậc hai của 3",                 "1.73205080756888",  "1.73205080756888", "—",          "Toán học", "#FFEBEE",
             "Chiều cao của tam giác đều có độ dài cạnh bằng 2"),
            ("ln2",   "Logarit tự nhiên của 2",            "0.69314718055995",  "0.69314718055995", "—",          "Toán học", "#FFEBEE",
             "Liên hệ hằng số phân rã với chu kỳ bán rã: T_{1/2} = ln2 / λ"),
            ("γ",     "Hằng số Euler-Mascheroni",          "0.57721566490153",  "0.57721566490153", "—",          "Toán học", "#FFEBEE",
             "Giới hạn của hiệu tổng điều hòa và logarit tự nhiên"),
        };

        // ═══════════════════════════════════════════════════════════
        //  CATEGORY TABS
        // ═══════════════════════════════════════════════════════════

        private void BuildCategoryTabs()
        {
            if (categoryPanel == null) return;
            categoryPanel.Children.Clear();

            var categories = new (string Name, string Icon, string Color)[]
            {
                ("Tất cả",    "📋", "#424242"),
                ("Vật lý",    "⚡", "#1565C0"),
                ("Hóa học",   "🧪", "#2E7D32"),
                ("Hạt nhân",  "⚛️", "#7B1FA2"),
                ("Thiên văn", "🌌", "#E65100"),
                ("Toán học",  "📐", "#C62828"),
            };

            foreach (var (name, icon, colorHex) in categories)
            {
                var color = (Color)ColorConverter.ConvertFromString(colorHex);
                bool isActive = name == _activeCategory;

                var tab = new Border
                {
                    Background = isActive
                        ? new SolidColorBrush(color)
                        : new SolidColorBrush(Color.FromArgb(15, color.R, color.G, color.B)),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(16, 8, 16, 8),
                    Margin = new Thickness(0, 0, 6, 6),
                    Cursor = Cursors.Hand,
                    BorderBrush = new SolidColorBrush(Color.FromArgb(50, color.R, color.G, color.B)),
                    BorderThickness = isActive ? new Thickness(2) : new Thickness(1),
                    MinHeight = DS.TouchMinHeight,
                    VerticalAlignment = VerticalAlignment.Center
                };

                int count = name == "Tất cả"
                    ? AllConstants.Count
                    : AllConstants.Count(c => c.Category == name);

                var sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                sp.Children.Add(new TextBlock { 
                    Text = $"{icon} {name}", 
                    FontSize = 14,
                    FontFamily = DS.FontPrimary,
                    FontWeight = isActive ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = isActive ? Brushes.White : new SolidColorBrush(color),
                    VerticalAlignment = VerticalAlignment.Center
                });
                sp.Children.Add(new TextBlock { 
                    Text = $" ({count})", 
                    FontSize = 12,
                    FontFamily = DS.FontPrimary,
                    Foreground = isActive ? new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)) : Brushes.Gray,
                    VerticalAlignment = VerticalAlignment.Center
                });
                tab.Child = sp;

                string capturedName = name;
                tab.MouseLeftButtonDown += (_, _) =>
                {
                    _activeCategory = capturedName;
                    BuildCategoryTabs();
                    RenderConstants(capturedName, txtSearch?.Text);
                };

                tab.MouseEnter += (_, _) =>
                {
                    if (capturedName != _activeCategory)
                        tab.Background = new SolidColorBrush(Color.FromArgb(30, color.R, color.G, color.B));
                };
                tab.MouseLeave += (_, _) =>
                {
                    if (capturedName != _activeCategory)
                        tab.Background = new SolidColorBrush(Color.FromArgb(15, color.R, color.G, color.B));
                };

                categoryPanel.Children.Add(tab);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  RENDER CONSTANTS
        // ═══════════════════════════════════════════════════════════

        private void Search_Changed(object sender, TextChangedEventArgs e)
        {
            RenderConstants(_activeCategory, txtSearch?.Text);
        }

        private void RenderConstants(string category, string? search)
        {
            if (constantsPanel == null) return;
            constantsPanel.Children.Clear();

            var filtered = AllConstants.AsEnumerable();

            if (category != "Tất cả")
                filtered = filtered.Where(c => c.Category == category);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.ToLowerInvariant();
                filtered = filtered.Where(c =>
                    c.Symbol.ToLowerInvariant().Contains(q) ||
                    c.Name.ToLowerInvariant().Contains(q));
            }

            var list = filtered.ToList();

            if (list.Count == 0)
            {
                constantsPanel.Children.Add(new TextBlock
                {
                    Text = "Không tìm thấy hằng số nào.",
                    FontSize = 14, 
                    FontFamily = DS.FontPrimary,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                });
                return;
            }

            foreach (var (symbol, name, value, copyValue, unit, cat, colorHex, desc) in list)
            {
                var cardBg = (SolidColorBrush)new BrushConverter().ConvertFrom(colorHex)!;

                var card = new Border
                {
                    Background = cardBg,
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(16, 12, 16, 12),
                    Margin = new Thickness(0, 0, 8, 8),
                    Width = 265, // Đảm bảo không vỡ dòng cho các số thập phân dài
                    Cursor = Cursors.Hand,
                    BorderBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
                    BorderThickness = new Thickness(1),
                    ToolTip = $"{desc}\n\nNhấp chuột trái: Copy giá trị tính toán ({copyValue})\nNhấp chuột phải: Copy công thức ({symbol} = {value} {unit})"
                };

                var sp = new StackPanel();

                // Symbol + Category badge
                var headerDock = new DockPanel();
                headerDock.Children.Add(new TextBlock
                {
                    Text = symbol, 
                    FontSize = 26, 
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                    FontFamily = DS.FontBold
                });

                var catBadge = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(6, 2, 6, 2),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                catBadge.Child = new TextBlock
                {
                    Text = cat, 
                    FontSize = 11, 
                    FontFamily = DS.FontPrimary,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                DockPanel.SetDock(catBadge, Dock.Right);
                headerDock.Children.Insert(0, catBadge);

                sp.Children.Add(headerDock);

                sp.Children.Add(new TextBlock
                {
                    Text = name, 
                    FontSize = 15,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = DS.Brush(DS.TextPrimary),
                    FontFamily = DS.FontPrimary,
                    Margin = new Thickness(0, 4, 0, 4)
                });

                sp.Children.Add(new TextBlock
                {
                    Text = $"{symbol} = {value} {unit}",
                    FontSize = 16, 
                    FontWeight = FontWeights.Bold,
                    Foreground = DS.Brush(DS.BrandPrimary),
                    TextWrapping = TextWrapping.Wrap,
                    FontFamily = DS.FontMath
                });

                card.Child = sp;

                // Copy on Left Click (Computer-parseable clean value)
                string copyVal = copyValue;
                card.MouseLeftButtonDown += (_, _) =>
                {
                    try
                    {
                        Clipboard.SetText(copyVal);
                        card.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                        card.BorderThickness = new Thickness(2);
                        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
                        timer.Tick += (_, _) =>
                        {
                            card.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0));
                            card.BorderThickness = new Thickness(1);
                            timer.Stop();
                        };
                        timer.Start();
                    }
                    catch { }
                };

                // Context Menu for Alternate Copy formats (Right Click)
                var contextMenu = new ContextMenu();
                
                var copyNumItem = new MenuItem { Header = $"Copy giá trị tính toán ({copyValue})" };
                copyNumItem.Click += (s, e) => { try { Clipboard.SetText(copyValue); } catch { } };
                
                var copyExprItem = new MenuItem { Header = $"Copy công thức đầy đủ ({symbol} = {value} {unit})" };
                copyExprItem.Click += (s, e) => { try { Clipboard.SetText($"{symbol} = {value} {unit}"); } catch { } };
                
                contextMenu.Items.Add(copyNumItem);
                contextMenu.Items.Add(copyExprItem);
                card.ContextMenu = contextMenu;

                // Hover Effects
                card.MouseEnter += (_, _) =>
                {
                    card.BorderBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                    card.BorderThickness = new Thickness(2);
                };
                card.MouseLeave += (_, _) =>
                {
                    card.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0));
                    card.BorderThickness = new Thickness(1);
                };

                // Wrap with section toolbar - Unique target ID to prevent focus conflicts
                string secId = $"{NormalizeString(cat)}_{NormalizeString(symbol)}";
                constantsPanel.Children.Add(WrapWithSectionToolbar(card, "constants", secId, $"{symbol} — {name}"));
            }
        }

        // Helper method to generate unique and safe section IDs for the teaching toolbar
        private static string RemoveSign(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            string[] arr1 = new string[] { "á", "à", "ả", "ã", "ạ", "â", "ấ", "ầ", "ẩ", "ẫ", "ậ", "ă", "ắ", "ằ", "ẳ", "ẵ", "ặ",
                "đ", "é", "è", "ẻ", "ẽ", "ẹ", "ê", "ế", "ề", "ể", "ễ", "ệ", "í", "ì", "ỉ", "ĩ", "ị", "ó", "ò", "ỏ", "õ", "ọ", "ô", "ố", "ồ", "ổ", "ỗ", "ộ", "ơ", "ớ", "ờ", "ở", "ỡ", "ợ",
                "ú", "ù", "ủ", "ũ", "ụ", "ư", "ứ", "ừ", "ử", "ữ", "ự", "ý", "ỳ", "ỷ", "ỹ", "ỵ",
                "Á", "À", "Ả", "Ã", "Ạ", "Â", "Ấ", "Ầ", "Ẩ", "Ẫ", "Ậ", "Ă", "Ắ", "Ằ", "Ẳ", "Ẵ", "Ặ",
                "Đ", "É", "È", "Ẻ", "Ẽ", "Ẹ", "Ê", "Ế", "Ề", "Ể", "Ễ", "Ệ", "Í", "Ì", "Ỉ", "Ĩ", "Ị", "Ó", "Ò", "Ỏ", "Õ", "Ọ", "Ô", "Ố", "Ồ", "Ổ", "Ỗ", "Ộ", "Ơ", "Ớ", "Ờ", "Ở", "Ỡ", "Ợ",
                "Ú", "Ù", "Ủ", "Ũ", "Ụ", "Ư", "Ứ", "Ừ", "Ử", "Ữ", "Ự", "Ý", "Ỳ", "Ỷ", "Ỹ", "Ý" };
            string[] arr2 = new string[] { "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a",
                "d", "e", "e", "e", "e", "e", "e", "e", "e", "e", "e", "e", "i", "i", "i", "i", "i", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o",
                "u", "u", "u", "u", "u", "u", "u", "u", "u", "u", "u", "y", "y", "y", "y", "y",
                "A", "A", "A", "A", "A", "A", "A", "A", "A", "A", "A", "A", "A", "A", "A", "A", "A",
                "D", "E", "E", "E", "E", "E", "E", "E", "E", "E", "E", "E", "I", "I", "I", "I", "I", "O", "O", "O", "O", "O", "O", "O", "O", "O", "O", "O", "O", "O", "O", "O", "O", "O",
                "U", "U", "U", "U", "U", "U", "U", "U", "U", "U", "U", "Y", "Y", "Y", "Y", "Y" };
            for (int i = 0; i < arr1.Length; i++)
            {
                text = text.Replace(arr1[i], arr2[i]);
            }
            return text;
        }

        private static string NormalizeString(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "const";
            string temp = RemoveSign(input)
                .Replace("G", "g_cap")
                .ToLowerInvariant()
                .Replace(" ", "_")
                .Replace("μ₀", "mu_0")
                .Replace("ε₀", "ep_0")
                .Replace("ℏ", "hbar")
                .Replace("π", "pi")
                .Replace("φ", "phi")
                .Replace("√", "sqrt")
                .Replace("☉", "_sun")
                .Replace("⊕", "_earth");
            return temp;
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
                        Icon = "📡",
                        Title = isVN ? "Định Vị Toàn Cầu GPS" : "GPS Time Synchronization",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_constants_1_{suffix}.png",
                        Description = isVN 
                            ? "Tốc độ ánh sáng (c) giúp các vệ tinh tính toán khoảng cách cực kỳ chính xác đến thiết bị di động dựa trên độ trễ thời gian truyền tín hiệu vô tuyến." 
                            : "Synchronize atomic clocks in GPS satellites using Einstein's relativity formulas incorporating the speed of light (c)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "☀",
                        Title = isVN ? "️ Pin Mặt Trời & LED" : "Quantum Physics & Photoelectric",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_constants_2_{suffix}.png",
                        Description = isVN 
                            ? "Hằng số Planck (h) là chìa khóa lượng tử mô tả mối liên hệ giữa năng lượng ánh sáng và hạt tải điện, nền tảng của các cảm biến quang điện." 
                            : "Calculate photon energy (E = h * f) in solar cells and light sensors using Planck's constant (h)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🚀",
                        Title = isVN ? "Quỹ Đạo Vũ Trụ" : "Space Orbit Design",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_constants_3_{suffix}.png",
                        Description = isVN 
                            ? "Hằng số hấp dẫn (G) được dùng để định vị, thiết lập quỹ đạo vệ tinh nhân tạo quanh Trái Đất và tính toán lực hấp dẫn trong vũ trụ." 
                            : "Calculate planetary trajectories and satellite orbits using Newton's gravitational constant (G)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🔋",
                        Title = isVN ? "Xi Mạ Điện Hóa" : "Electrochemical Electroplating",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_constants_4_{suffix}.png",
                        Description = isVN 
                            ? "Hằng số Faraday (F) biểu diễn điện lượng một mol electron, dùng để kiểm soát lượng đồng/vàng bám vào vật liệu mạ điện phân trong nhà máy." 
                            : "Faraday's constant (F) represents the electric charge of one mole of electrons, used to control the amount of copper/gold adhering to electroplated materials in factories."
                    },
                    new PracticalAppItem
                    {
                        Icon = "⚙",
                        Title = isVN ? "️ Bánh Răng Truyền Động" : "Mechanical Gear Transmission",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_constants_5_{suffix}.png",
                        Description = isVN 
                            ? "Số Pi (π) giúp chế tạo bánh răng cơ khí, thiết kế đường tròn đồng tâm và tính toán chu kỳ quay của các chi tiết động cơ phản lực." 
                            : "The number Pi (π) helps manufacture mechanical gears, design concentric circles, and calculate rotation cycles of jet engine parts."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📉",
                        Title = isVN ? "Phóng Xạ & Lãi Kép" : "Radioactive Decay & Compound Interest",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_constants_6_{suffix}.png",
                        Description = isVN 
                            ? "Số Euler (e) là cơ số tự nhiên biểu diễn tốc độ phân rã phóng xạ của uranium, sự phát triển quần thể và mô hình lãi kép tài chính." 
                            : "Euler's number (e) is the natural base representing the radioactive decay rate of uranium, population growth, and financial compound interest models."
                    },
                    new PracticalAppItem
                    {
                        Icon = "☀️",
                        Title = isVN ? "Bước sóng bức xạ cực tím" : "Ultraviolet Radiation Wavelength",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_constants_7_{suffix}.png",
                        Description = isVN 
                            ? "Sử dụng hằng số Planck để tính toán mức năng lượng và bước sóng của tia cực tím nhằm thiết kế thiết bị lọc tia UV." 
                            : "Use Planck's constant to calculate UV energy levels and wavelengths to design effective UV filtering devices."
                    },
                    new PracticalAppItem
                    {
                        Icon = "☢️",
                        Title = isVN ? "Chu kỳ bán rã hạt nhân" : "Nuclear Half-life Decay",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_constants_8_{suffix}.png",
                        Description = isVN 
                            ? "Áp dụng hằng số phân rã phóng xạ để tính toán thời gian suy giảm lượng chất hạt nhân trong y tế và địa chất học." 
                            : "Apply the radioactive decay constant to calculate the decay timeline of nuclear isotopes in medicine and geology."
                    }};

                practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for ConstantsTool: {Err}", ex.Message);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewLookup == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewLookup.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewLookup.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
