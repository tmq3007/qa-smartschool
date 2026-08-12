using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Views;
using QASmartClass.LearningTools.Models;

namespace QASmartClass.LearningTools.Views.Multi
{
    public partial class FormulasTool : BaseToolControl
    {
        private string _activeSubject = "Tất cả";
        private int _quizCurrentQuestionIndex = 0;
        private int _quizScore = 0;
        private const int QuizTotalQuestions = 5;

        public FormulasTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                BuildSubjectTabs();
                RenderFormulas("Tất cả", null);
                TouchTextPad.Attach(txtSearch, mode: "text");
                if (txtSearchPlaceholder != null)
                {
                    txtSearchPlaceholder.Visibility = string.IsNullOrEmpty(txtSearch.Text) ? Visibility.Visible : Visibility.Collapsed;
                }
                LoadPracticalApps();

                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Ghi chú" : "Study Guide & Notes";
                if (menuTextFormulas != null) menuTextFormulas.Text = isVN ? "Bảng công thức" : "Formula Board";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                if (sideMenu != null)
                {
                    sideMenu.SelectedIndex = 0; // Default to formula board
                }
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  DATA — 50+ công thức, phân loại theo môn + chủ đề
        // ═══════════════════════════════════════════════════════════

        private static readonly Dictionary<string, List<(string Topic, string Name, string Formula, string Desc, string Grade, string? LinkedToolKey)>> AllFormulas = new()
        {
            ["🔬 Vật lý"] = new()
            {
                // Cơ học
                ("Cơ học", "Tốc độ (Vận tốc thẳng)", "v = s / t",                  "v: tốc độ, s: quãng đường, t: thời gian",        "Lớp 8", null),
                ("Cơ học", "Gia tốc",              "a = (v − v₀) / t",          "a: gia tốc, v₀: vận tốc đầu",                     "Lớp 10", null),
                ("Cơ học", "Độ lớn lực — Newton II", "F = m·a",                   "F: độ lớn lực, m: khối lượng, a: gia tốc",         "Lớp 10", null),
                ("Cơ học", "Độ lớn trọng lực",      "P = m·g",                   "P: độ lớn trọng lực, g ≈ 9.8 m/s²",                "Lớp 8", null),
                ("Cơ học", "Rơi tự do",             "s = ½·g·t²",                "Quãng đường rơi tự do, v₀ = 0",                    "Lớp 10", null),
                ("Cơ học", "Chuyển động ném xiên",   "y = x·tanα − g·x² / (2·v₀²·cos²α)", "Quỹ đạo parabol",                             "Lớp 10", null),

                // Năng lượng
                ("Năng lượng", "Động năng",          "W_đ = ½·m·v²",              "W_đ: động năng, m: khối lượng, v: vận tốc",        "Lớp 10", null),
                ("Năng lượng", "Thế năng",           "W_t = m·g·h",               "W_t: thế năng, h: độ cao so với gốc",              "Lớp 10", null),
                ("Năng lượng", "Công",               "A = F · s · cosα",          "α: góc giữa lực và phương chuyển động",            "Lớp 10", null),
                ("Năng lượng", "Công suất",           "P = A / t = F · v",         "A: công, t: thời gian",                            "Lớp 10", null),

                // Điện
                ("Điện", "Định luật Ohm",            "I = U / R",                "U: hiệu điện thế, R: điện trở",                    "Lớp 9", "circuit"),
                ("Điện", "Điện trở",                 "R = ρ·l / S",              "ρ: điện trở suất, l: chiều dài, S: tiết diện",     "Lớp 9", null),
                ("Điện", "Công suất điện",           "P = U·I = I²·R = U² / R",   "3 dạng tương đương",                               "Lớp 9", null),
                ("Điện", "Điện năng tiêu thụ",       "A = P·t = U·I·t",          "A: điện năng, t: thời gian",                       "Lớp 9", null),
                ("Điện", "Định luật Coulomb",        "F = k · |q₁ · q₂| / (ε · r²)", "k = 9·10⁹ N·m²/C², ε: điện môi",                   "Lớp 11", null),

                // Sóng & Quang
                ("Sóng", "Bước sóng",               "λ = v / f",                 "v: tốc độ sóng, f: tần số",                        "Lớp 11", null),
                ("Sóng", "Tần số",                  "f = 1 / T",                 "T: chu kỳ",                                        "Lớp 11", null),
                ("Sóng", "Năng lượng photon",       "E = h·f = h·c / λ",         "h: hằng số Planck",                                "Lớp 12", null),
                ("Sóng", "Einstein quang điện",      "h·f = A + ½·m·v²_max",      "A: công thoát electron",                            "Lớp 12", null),

                // Nhiệt
                ("Nhiệt", "Nhiệt lượng",            "Q = m·c·Δt",                "c: nhiệt dung riêng, Δt: biến thiên nhiệt độ",    "Lớp 8", null),
                ("Nhiệt", "Phương trình trạng thái","PV/T = hằng số",             "Khí lý tưởng — Clapeyron",                        "Lớp 10", null),
            },

            ["🧪 Hóa học"] = new()
            {
                ("Cơ bản", "Số mol",                 "n = m / M",                 "m: khối lượng, M: khối lượng mol",                 "Lớp 8", null),
                ("Cơ bản", "Số mol (thể tích)",      "n = V / 24.79 (đkc)",       "V: thể tích khí ở đkc (25°C, 1 bar)",              "Lớp 8", null),
                ("Cơ bản", "Khối lượng chất",        "m = n·M",                   "Tính khối lượng từ số mol",                        "Lớp 8", null),

                ("Dung dịch", "Nồng độ mol",         "C_M = n / V",               "n: số mol, V: thể tích dung dịch (lít)",          "Lớp 8", null),
                ("Dung dịch", "Nồng độ %",           "C% = m_ct / m_dd · 100%",    "m_ct: chất tan, m_dd: dung dịch",                 "Lớp 8", null),
                ("Dung dịch", "Mối liên hệ CM và C%","C_M = 10·D·C% / M",         "D: khối lượng riêng",                              "Lớp 10", null),

                ("Nhiệt động", "PV = nRT",            "P·V = n·R·T",               "R = 8.314 J/(mol·K)",                              "Lớp 10", null),
                ("Nhiệt động", "Entanpi",             "ΔH = Σ(sp) − Σ(tc)",       "sp: sản phẩm, tc: tác chất",                      "Lớp 10", null),
                ("Nhiệt động", "Hằng số cân bằng",   "Kc = [sp]ⁿ / [tc]ᵐ",       "Ở trạng thái cân bằng",                           "Lớp 11", null),

                ("Axit-Bazơ", "pH",                   "pH = −log[H⁺]",            "[H⁺]: nồng độ ion H⁺",                            "Lớp 11", "ph_scale"),
                ("Axit-Bazơ", "pOH",                  "pOH = −log[OH⁻]",          "pH + pOH = 14 (ở 25°C)",                          "Lớp 11", null),

                ("Điện hóa", "Faraday",               "m = A·I·t / (n·F)",        "A: khối lượng mol, n: hóa trị, F: 96485 C/mol",   "Lớp 12", null),
                ("Tốc độ", "Tốc độ phản ứng",        "v = |ΔC| / Δt",            "ΔC: biến thiên nồng độ",                           "Lớp 10", null),
            },

            ["📐 Toán học"] = new()
            {
                ("Đại số", "PT bậc 2",              "x = (−b ± √Δ) / 2a",        "Δ = b² − 4ac",                                    "Lớp 9", "quadratic"),
                ("Đại số", "Vieta",                  "x₁ + x₂ = −b / a, x₁·x₂ = c / a", "Hệ thức Vieta cho PT bậc 2",                "Lớp 9", null),
                ("Đại số", "Logarit tích",           "logₐ(x·y) = logₐx + logₐy", "Tính chất logarit",                               "Lớp 11", null),
                ("Đại số", "Logarit thương",         "logₐ(x / y) = logₐx − logₐy", "Tính chất logarit",                              "Lớp 11", null),
                ("Đại số", "Đổi cơ số log",          "logₐb = log_c(b) / log_c(a)", "Công thức đổi cơ số",                            "Lớp 11", null),
                ("Đại số", "Lãi suất kép",          "A = P·(1 + r)ⁿ",            "P: vốn, r: lãi suất/kỳ, n: số kỳ",               "Lớp 10", null),

                ("Hình học", "Pythagoras",           "a² + b² = c²",              "Tam giác vuông: c là cạnh huyền",                  "Lớp 7", null),
                ("Hình học", "Diện tích tam giác",   "S = ½·a·h",                 "a: đáy, h: chiều cao",                             "Lớp 5", null),
                ("Hình học", "Diện tích tam giác đều", "S = a²·√3 / 4",             "a: cạnh tam giác đều",                             "Lớp 9", null),
                ("Hình học", "Diện tích tam giác vuông","S = ½·a·b",               "a, b: hai cạnh góc vuông",                         "Lớp 9", null),
                ("Hình học", "Diện tích hình tròn",  "S = π·r²",                  "r: bán kính",                                      "Lớp 5", "geometry"),
                ("Hình học", "Chu vi hình tròn",     "C = 2·π·r = π·d",           "r: bán kính, d: đường kính",                       "Lớp 5", "geometry"),
                ("Hình học", "Thể tích hình cầu",    "V = 4/3·π·r³",              "r: bán kính",                                      "Lớp 9", "geometry"),
                ("Hình học", "Thể tích hình trụ",    "V = π·r²·h",                "r: bán kính, h: chiều cao",                        "Lớp 9", "geometry"),
                ("Hình học", "Thể tích hình nón",    "V = π·r²·h / 3",            "r: bán kính đáy, h: chiều cao",                    "Lớp 9", "geometry"),
                ("Hình học", "Heron",                "S = √[p·(p − a)·(p − b)·(p − c)]", "p = (a+b+c)/2 — nửa chu vi",                  "Lớp 9", null),

                ("Lượng giác", "Hệ thức cơ bản",    "sin²α + cos²α = 1",         "Đúng với mọi α",                                   "Lớp 10", null),
                ("Lượng giác", "Công thức nhân đôi", "sin(2α) = 2·sinα·cosα",     "cos(2α) = cos²α − sin²α",                          "Lớp 11", null),
                ("Lượng giác", "Định lý sin",        "a / sinA = b / sinB = c / sinC = 2·R", "R: bán kính ngoại tiếp",                 "Lớp 10", null),
                ("Lượng giác", "Định lý cos",        "c² = a² + b² − 2·a·b·cosC",  "Tổng quát hóa Pythagoras",                         "Lớp 10", null),

                ("Giải tích", "Đạo hàm lũy thừa",   "(xⁿ)' = n·xⁿ⁻¹",            "Công thức đạo hàm cơ bản",                         "Lớp 11", "derivative"),
                ("Giải tích", "Đạo hàm tích",        "(u·v)' = u'·v + u·v'",      "u, v là hàm của x",                                "Lớp 11", null),
                ("Giải tích", "Đạo hàm thương",      "(u / v)' = (u'·v − u·v') / v²", "v ≠ 0",                                        "Lớp 11", null),
                ("Giải tích", "Tích phân cơ bản",    "∫xⁿdx = xⁿ⁺¹ / (n + 1) + C", "n ≠ −1",                                          "Lớp 12", "integral"),
                ("Giải tích", "Tích phân xác định",  "∫ₐᵇf(x)dx = F(b) − F(a)",  "Newton-Leibniz",                                    "Lớp 12", "integral"),
            },
        };

        // ═══════════════════════════════════════════════════════════
        //  SUBJECT TABS
        // ═══════════════════════════════════════════════════════════

        private void BuildSubjectTabs()
        {
            if (subjectPanel == null) return;
            subjectPanel.Children.Clear();

            var subjects = new (string Name, string Color)[]
            {
                ("Tất cả",      "#424242"),
                ("⭐ Yêu thích", "#FFB300"),
                ("🔬 Vật lý",   "#1565C0"),
                ("🧪 Hóa học",  "#2E7D32"),
                ("📐 Toán học",  "#E65100"),
            };

            foreach (var (name, colorHex) in subjects)
            {
                var color = (Color)ColorConverter.ConvertFromString(colorHex);
                bool isActive = name == _activeSubject;

                int count = 0;
                if (name == "Tất cả")
                {
                    count = AllFormulas.Values.Sum(l => l.Count);
                }
                else if (name == "⭐ Yêu thích")
                {
                    var favList = FormulaFavoritesTracker.GetFavorites();
                    count = AllFormulas.Values.SelectMany(l => l).Count(f => favList.Contains(f.Name));
                }
                else
                {
                    count = AllFormulas.ContainsKey(name) ? AllFormulas[name].Count : 0;
                }

                var tab = new Border
                {
                    Background = isActive
                        ? new SolidColorBrush(color)
                        : new SolidColorBrush(Color.FromArgb(15, color.R, color.G, color.B)),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(14, 8, 14, 8),
                    Margin = new Thickness(0, 0, 6, 6),
                    Cursor = Cursors.Hand,
                    BorderBrush = new SolidColorBrush(Color.FromArgb(50, color.R, color.G, color.B)),
                    BorderThickness = isActive ? new Thickness(2) : new Thickness(1)
                };

                var dp = new StackPanel { Orientation = Orientation.Horizontal };
                dp.Children.Add(new TextBlock
                {
                    Text = name, FontSize = 14,
                    FontWeight = isActive ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = isActive ? Brushes.White : new SolidColorBrush(color)
                });
                dp.Children.Add(new TextBlock
                {
                    Text = $" ({count})", FontSize = 11,
                    Foreground = isActive ? new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)) : Brushes.Gray,
                    VerticalAlignment = VerticalAlignment.Center
                });
                tab.Child = dp;

                string capturedName = name;
                tab.MouseLeftButtonDown += (_, _) =>
                {
                    _activeSubject = capturedName;
                    BuildSubjectTabs();
                    RenderFormulas(capturedName, txtSearch?.Text);
                };

                tab.MouseEnter += (_, _) =>
                {
                    if (capturedName != _activeSubject)
                        tab.Background = new SolidColorBrush(Color.FromArgb(30, color.R, color.G, color.B));
                };
                tab.MouseLeave += (_, _) =>
                {
                    if (capturedName != _activeSubject)
                        tab.Background = new SolidColorBrush(Color.FromArgb(15, color.R, color.G, color.B));
                };

                subjectPanel.Children.Add(tab);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  RENDER FORMULAS
        // ═══════════════════════════════════════════════════════════

        private void Search_Changed(object sender, TextChangedEventArgs e)
        {
            if (txtSearchPlaceholder != null)
            {
                txtSearchPlaceholder.Visibility = string.IsNullOrEmpty(txtSearch.Text) ? Visibility.Visible : Visibility.Collapsed;
            }
            string rawSearch = txtSearch?.Text ?? "";
            if (rawSearch.Length > 100)
            {
                rawSearch = rawSearch.Substring(0, 100);
            }
            string sanitizedSearch = System.Text.RegularExpressions.Regex.Replace(rawSearch, @"[\[\]\\\*\?]", "");
            RenderFormulas(_activeSubject, sanitizedSearch);
        }

        private static readonly Dictionary<string, (string Bg, string Fg, string CardBg)> SubjectStyles = new()
        {
            ["🔬 Vật lý"]  = ("#E3F2FD", "#1565C0", "#F8FAFE"),
            ["🧪 Hóa học"] = ("#E8F5E9", "#2E7D32", "#F5FBF5"),
            ["📐 Toán học"] = ("#FFF3E0", "#E65100", "#FFFCF5"),
        };

        private void RenderFormulas(string subject, string? search)
        {
            if (formulasSections == null) return;
            formulasSections.Children.Clear();

            var favList = FormulaFavoritesTracker.GetFavorites();
            var subjectsToShow = (subject == "Tất cả" || subject == "⭐ Yêu thích")
                ? AllFormulas.Keys.ToList()
                : new List<string> { subject };

            foreach (var subj in subjectsToShow)
            {
                if (!AllFormulas.ContainsKey(subj)) continue;
                var formulas = AllFormulas[subj].AsEnumerable();

                if (subject == "⭐ Yêu thích")
                {
                    formulas = formulas.Where(f => favList.Contains(f.Name));
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var q = search.ToLowerInvariant();
                    var qClean = QASmartClass.Classroom.Services.VietnameseNameHelper.RemoveDiacritics(q);
                    formulas = formulas.Where(f =>
                        f.Name.ToLowerInvariant().Contains(q) ||
                        QASmartClass.Classroom.Services.VietnameseNameHelper.RemoveDiacritics(f.Name.ToLowerInvariant()).Contains(qClean) ||
                        f.Formula.ToLowerInvariant().Contains(q) ||
                        f.Desc.ToLowerInvariant().Contains(q) ||
                        QASmartClass.Classroom.Services.VietnameseNameHelper.RemoveDiacritics(f.Desc.ToLowerInvariant()).Contains(qClean) ||
                        f.Topic.ToLowerInvariant().Contains(q) ||
                        QASmartClass.Classroom.Services.VietnameseNameHelper.RemoveDiacritics(f.Topic.ToLowerInvariant()).Contains(qClean));
                }

                var list = formulas.ToList();
                if (list.Count == 0) continue;

                var (bgHex, fgHex, cardBgHex) = SubjectStyles.ContainsKey(subj)
                    ? SubjectStyles[subj] : ("#F5F5F5", "#424242", "#FFFFFF");
                var fgColor = (Color)ColorConverter.ConvertFromString(fgHex);

                // Subject header
                var header = new Border
                {
                    CornerRadius = new CornerRadius(12, 12, 0, 0),
                    Padding = new Thickness(16, 10, 16, 10),
                    Margin = new Thickness(0, 10, 0, 0),
                    Background = (SolidColorBrush)new BrushConverter().ConvertFrom(bgHex)!
                };
                header.Child = new TextBlock
                {
                    Text = $"{subj}  ({list.Count} công thức)",
                    FontSize = 18, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(fgColor)
                };
                formulasSections.Children.Add(header);

                // Group by topic
                var groups = list.GroupBy(f => f.Topic);
                foreach (var group in groups)
                {
                    // Topic label
                    formulasSections.Children.Add(new TextBlock
                    {
                        Text = $"📂 {group.Key}",
                        FontSize = 14, FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                        Margin = new Thickness(12, 10, 0, 6)
                    });

                    var wrap = new WrapPanel { Margin = new Thickness(4, 0, 0, 0) };

                    foreach (var (topic, name, formula, desc, grade, linkedToolKey) in group)
                    {
                        var cardBgColor = (Color)ColorConverter.ConvertFromString(cardBgHex);
                        var card = new Border
                        {
                            Background = new SolidColorBrush(cardBgColor),
                            CornerRadius = new CornerRadius(10),
                            Padding = new Thickness(14, 10, 14, 10),
                            Margin = new Thickness(0, 0, 8, 8),
                            Width = 230, Cursor = Cursors.Hand,
                            BorderBrush = new SolidColorBrush(Color.FromArgb(40, fgColor.R, fgColor.G, fgColor.B)),
                            BorderThickness = new Thickness(1),
                            ToolTip = $"Click copy • Double-click xem chi tiết\n{formula} — {desc}"
                        };

                        var sp = new StackPanel();

                        // Name + Grade badge
                        var nameDock = new DockPanel();
                        var gradeBadge = new Border
                        {
                            Background = new SolidColorBrush(Color.FromArgb(20, fgColor.R, fgColor.G, fgColor.B)),
                            CornerRadius = new CornerRadius(4),
                            Padding = new Thickness(5, 1, 5, 1),
                            HorizontalAlignment = HorizontalAlignment.Right,
                            VerticalAlignment = VerticalAlignment.Top
                        };
                        gradeBadge.Child = new TextBlock
                        {
                            Text = grade, FontSize = 10,
                            Foreground = new SolidColorBrush(fgColor)
                        };
                        DockPanel.SetDock(gradeBadge, Dock.Right);
                        nameDock.Children.Add(gradeBadge);

                        // Star favorite button
                        bool isFav = FormulaFavoritesTracker.IsFavorite(name);
                        var starBtn = new TextBlock
                        {
                            Text = isFav ? "★" : "☆",
                            FontSize = 16,
                            Foreground = new SolidColorBrush(isFav ? Color.FromRgb(255, 179, 0) : Color.FromRgb(189, 189, 189)),
                            Cursor = Cursors.Hand,
                            Margin = new Thickness(0, 0, 6, 0),
                            VerticalAlignment = VerticalAlignment.Center,
                            ToolTip = isFav ? "Bỏ yêu thích công thức" : "Yêu thích công thức"
                        };
                        string capNameForStar = name;
                        starBtn.MouseLeftButtonDown += (s, ev) =>
                        {
                            ev.Handled = true;
                            bool nowFav = FormulaFavoritesTracker.ToggleFavorite(capNameForStar);
                            starBtn.Text = nowFav ? "★" : "☆";
                            starBtn.Foreground = new SolidColorBrush(nowFav ? Color.FromRgb(255, 179, 0) : Color.FromRgb(189, 189, 189));
                            starBtn.ToolTip = nowFav ? "Bỏ yêu thích công thức" : "Yêu thích công thức";
                            BuildSubjectTabs();
                        };
                        DockPanel.SetDock(starBtn, Dock.Right);
                        nameDock.Children.Add(starBtn);

                        nameDock.Children.Add(new TextBlock
                        {
                            Text = name, FontSize = 13, FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(fgColor)
                        });
                        sp.Children.Add(nameDock);

                        // Formula
                        sp.Children.Add(new TextBlock
                        {
                            Text = formula, FontSize = 18, FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Color.FromRgb(26, 35, 126)),
                            FontFamily = Application.Current?.TryFindResource("InterFont") as FontFamily ?? new FontFamily("Segoe UI"),
                            Margin = new Thickness(0, 4, 0, 4),
                            TextWrapping = TextWrapping.Wrap
                        });

                        // Description
                        sp.Children.Add(new TextBlock
                        {
                            Text = desc, FontSize = 12,
                            Foreground = Brushes.Gray,
                            TextWrapping = TextWrapping.Wrap
                        });

                        // "Xem chi tiết" hint + Fire view badge
                        var bottomDock = new DockPanel { Margin = new Thickness(0, 3, 0, 0) };
                        
                        var hintTb = new TextBlock
                        {
                            Text = "📋 Copy • 🔍 Chi tiết",
                            FontSize = 10, Foreground = new SolidColorBrush(Color.FromArgb(120, 0, 0, 0)),
                            FontStyle = FontStyles.Italic,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        bottomDock.Children.Add(hintTb);

                        TextBlock? viewsBadge = null;
                        int views = FormulaStatsManager.GetCount(name);
                        if (views > 0)
                        {
                            viewsBadge = new TextBlock
                            {
                                Text = $"🔥 {views}",
                                FontSize = 10, FontWeight = FontWeights.Bold,
                                Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0)),
                                HorizontalAlignment = HorizontalAlignment.Right,
                                VerticalAlignment = VerticalAlignment.Center
                            };
                            DockPanel.SetDock(viewsBadge, Dock.Right);
                            bottomDock.Children.Add(viewsBadge);
                        }
                        
                        sp.Children.Add(bottomDock);

                        card.Child = sp;

                        // Click (copy) and Double-click (detail overlay) separation
                        string copyFormula = formula;
                        string capName = name, capFormula = formula, capDesc = desc, capGrade = grade, capSubj = subj, capLinkedKey = linkedToolKey;
                        var badgeRef = viewsBadge;

                        card.MouseLeftButtonDown += (s, e) =>
                        {
                            if (e.ClickCount == 1)
                            {
                                try
                                {
                                    Clipboard.SetText(copyFormula);
                                    FormulaStatsManager.Increment(capName);
                                    PlayCopySound();

                                    if (badgeRef != null)
                                    {
                                        badgeRef.Text = $"🔥 {FormulaStatsManager.GetCount(capName)}";
                                    }
                                    else
                                    {
                                        RenderFormulas(_activeSubject, txtSearch?.Text);
                                    }

                                    card.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                                    card.BorderThickness = new Thickness(2);
                                    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
                                    timer.Tick += (_, _) =>
                                    {
                                        card.BorderBrush = new SolidColorBrush(Color.FromArgb(40, fgColor.R, fgColor.G, fgColor.B));
                                        card.BorderThickness = new Thickness(1);
                                        timer.Stop();
                                    };
                                    timer.Start();
                                }
                                catch { }
                            }
                            else if (e.ClickCount == 2)
                            {
                                ShowFormulaDetail(capName, capFormula, capDesc, capGrade, capSubj, capLinkedKey);
                                if (badgeRef != null)
                                {
                                    badgeRef.Text = $"🔥 {FormulaStatsManager.GetCount(capName)}";
                                }
                                else
                                {
                                    RenderFormulas(_activeSubject, txtSearch?.Text);
                                }
                            }
                        };

                        // Hover
                        card.MouseEnter += (_, _) =>
                        {
                            card.Background = new SolidColorBrush(Color.FromArgb(30, fgColor.R, fgColor.G, fgColor.B));
                        };
                        card.MouseLeave += (_, _) =>
                        {
                            card.Background = new SolidColorBrush(cardBgColor);
                        };

                        string secId = name.Replace(" ", "_").ToLowerInvariant();
                        wrap.Children.Add(WrapWithSectionToolbar(card, "formulas", secId, $"{name}: {formula}"));
                    }

                    formulasSections.Children.Add(wrap);
                }
            }

            if (formulasSections.Children.Count == 0)
            {
                formulasSections.Children.Add(new TextBlock
                {
                    Text = "Không tìm thấy công thức nào.",
                    FontSize = 12, Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                });
            }
        }
        // ═══════════════════════════════════════════════════════════
        //  DETAIL OVERLAY — Double-click xem chi tiết công thức
        // ═══════════════════════════════════════════════════════════

        private record FormulaInfo(string Variables, string Meaning, string RealWorld, string Example, string Note);

        private static readonly Dictionary<string, FormulaInfo> Details = new()
        {
            // ── VẬT LÝ: Cơ học ──
            ["Tốc độ (Vận tốc thẳng)"] = new("• v: tốc độ (m/s) hoặc vận tốc trong chuyển động thẳng không đổi chiều\n• s: quãng đường đi được (m)\n• t: thời gian di chuyển (s)",
                "Tốc độ cho biết mức độ nhanh chậm của chuyển động (đại lượng vô hướng). Trong chương trình GDPT 2018, Vận tốc được định nghĩa bằng độ dịch chuyển chia thời gian (đại lượng vectơ có hướng).",
                "• Đo tốc độ phương tiện giao thông (tốc kế)\n• Tính thời gian hành trình tàu xe\n• Lập kế hoạch di chuyển",
                "Xe máy đi được quãng đường 90 km trong thời gian 2 giờ:\nv = s/t = 90 / 2 = 45 km/h",
                "Đơn vị đo chuẩn trong SI là m/s. Để đổi sang km/h ta nhân với 3.6."),
            ["Gia tốc"] = new("• a: gia tốc (m/s²)\n• v: vận tốc cuối (m/s)\n• v₀: vận tốc đầu (m/s)\n• t: thời gian biến thiên (s)",
                "Gia tốc đo tốc độ thay đổi vận tốc của vật theo thời gian. Tính chất chuyển động:\n- Nhanh dần (tăng tốc) khi a và v cùng dấu (a·v > 0).\n- Chậm dần (giảm tốc) khi a và v ngược dấu (a·v < 0).",
                "• Ô tô nhấn ga tăng tốc để vượt xe khác\n• Xe máy bóp phanh giảm tốc độ khi gặp chướng ngại vật\n• Tàu vũ trụ tăng tốc bứt phá khỏi khí quyển",
                "Xe đang đi với tốc độ 10 m/s thì tăng tốc lên 20 m/s trong 5 giây:\na = (20 − 10) / 5 = 2 m/s²",
                "Gia tốc là đại lượng vectơ. Gia tốc tự do ở Trái Đất thường lấy g ≈ 9.8 m/s² hoặc 10 m/s²."),
            ["Độ lớn lực — Newton II"] = new("• F: lực tác dụng lên vật (N)\n• m: khối lượng của vật (kg)\n• a: gia tốc vật thu được (m/s²)",
                "Định luật II Newton phát biểu rằng gia tốc của một vật tỉ lệ thuận với lực tác dụng và tỉ lệ nghịch với khối lượng của vật.",
                "• Thiết kế hệ thống phanh xe dựa vào tải trọng\n• Tính lực đẩy động cơ cần thiết để phóng vệ tinh\n• Trò chơi kéo co",
                "Đẩy một thùng hàng nặng 50 kg đi với gia tốc 1.5 m/s²:\nF = m·a = 50 · 1.5 = 75 N",
                "Đơn vị của lực là Newton (N). 1 N = 1 kg·m/s²."),
            ["Độ lớn trọng lực"] = new("• P: trọng lực tác dụng lên vật (N)\n• m: khối lượng của vật (kg)\n• g: gia tốc rơi tự do (gấp ≈ 9.8 m/s² ở bề mặt Trái Đất)",
                "Trọng lực là lực hút của Trái Đất tác dụng lên vật, tạo ra gia tốc rơi tự do. Có phương thẳng đứng và hướng về tâm Trái Đất.",
                "• Xác định cân nặng thực tế của cơ thể\n• Tính toán kết cấu chịu lực của các cột nhà\n• Đo tải trọng cầu",
                "Một vật có khối lượng 5 kg ở mặt đất:\nP = m·g = 5 · 9.8 = 49 N",
                "Trọng lượng P thay đổi tùy vị trí địa lý do g thay đổi (lớn nhất ở địa cực, nhỏ nhất ở xích đạo và giảm dần khi lên cao)."),
            ["Rơi tự do"] = new("• s: quãng đường rơi tự do (m)\n• g: gia tốc rơi tự do (m/s²)\n• t: thời gian rơi từ lúc bắt đầu thả (s)",
                "Chuyển động rơi tự do là sự rơi của một vật chỉ dưới tác dụng của trọng lực (bỏ qua mọi sức cản không khí). Vận tốc tức thời v = g·t.",
                "• Thả rơi quả bóng từ ban công và đo thời gian để tính chiều cao ban công\n• Thiết kế hệ thống dù thoát hiểm\n• Tính thời gian rơi của quả táo",
                "Thả rơi một viên đá từ đỉnh tháp sau 4 giây chạm đất (g = 10 m/s²):\ns = ½·g·t² = ½ · 10 · 4² = 80 m",
                "Mọi vật ở cùng một nơi rơi tự do đều có gia tốc như nhau không phụ thuộc khối lượng."),
            ["Chuyển động ném xiên"] = new("• y: tọa độ độ cao (m)\n• x: tọa độ tầm xa (m)\n• α: góc ném ban đầu so với phương ngang (°)\n• v₀: vận tốc ban đầu (m/s)\n• g: gia tốc trọng trường (m/s²)",
                "Phương trình mô tả quỹ đạo chuyển động của vật được ném từ mặt đất với vận tốc ban đầu v₀ hướng xiên góc α so với phương ngang. Quỹ đạo có dạng parabol.",
                "• Quỹ đạo quả bóng rổ khi ném vào rổ\n• Vòi phun nước trang trí nghệ thuật\n• Pháo binh tính toán góc bắn",
                "Ném quả bóng với góc 45° từ mặt đất, v₀ = 10 m/s, g = 10 m/s²:\ny = x·tan(45°) − 10·x² / (2 · 10² · cos²(45°)) = x − 0.1·x²",
                "Tầm xa đạt cực đại khi góc ném ban đầu α = 45° (nếu bỏ qua sức cản không khí)."),
            ["Động năng"] = new("• W_đ: động năng của vật (J)\n• m: khối lượng của vật (kg)\n• v: vận tốc chuyển động (m/s)",
                "Động năng là dạng năng lượng mà một vật có được do nó đang chuyển động. Động năng tỉ lệ thuận với khối lượng và bình phương vận tốc.",
                "• Đánh giá mức độ va chạm giao thông ở các dải tốc độ khác nhau\n• Khai thác động năng gió để phát điện\n• Năng lượng búa đóng đinh",
                "Một ô tô có khối lượng 1000 kg chạy với tốc độ 20 m/s:\nW_đ = ½·m·v² = ½ · 1000 · 20² = 200,000 J = 200 kJ",
                "Vận tốc tăng gấp đôi thì động năng tăng gấp bốn lần. Đây là lý do tốc độ cao cực kỳ nguy hiểm khi va chạm."),
            ["Thế năng"] = new("• W_t: thế năng trọng trường (J)\n• m: khối lượng vật (kg)\n• g: gia tốc trọng trường (m/s²)\n• h: độ cao của vật so với mốc thế năng (m)",
                "Thế năng trọng trường là năng lượng tương tác giữa Trái Đất và vật, phụ thuộc vào vị trí của vật trong trọng trường.",
                "• Nước ở trên đập cao của nhà máy thủy điện tích lũy thế năng để làm quay tuabin\n• Con lắc đồng hồ dao động thế năng chuyển hóa thành động năng\n• Cầu trượt trẻ em",
                "Một chậu cây nặng 2 kg đặt ở ban công tầng 3 cao 10 m so với mặt đất (g = 9.8 m/s²):\nW_t = m·g·h = 2 · 9.8 · 10 = 196 J",
                "Thế năng có thể có giá trị âm hoặc dương tùy thuộc vào cách chọn mốc tọa độ làm mốc thế năng (h = 0)."),
            ["Công"] = new("• A: công cơ học của lực (J)\n• F: độ lớn của lực tác dụng (N)\n• s: độ dịch chuyển của vật (m)\n• α: góc hợp bởi lực tác dụng và hướng chuyển dịch của vật",
                "Công cơ học đo lượng năng lượng được truyền từ lực làm vật dịch chuyển. Góc α quyết định tính chất công:\n- α < 90°: công phát động (A > 0).\n- α = 90°: lực không sinh công (A = 0).\n- α > 90°: công cản (A < 0).",
                "• Đẩy một xe chở hàng đi trên đường phẳng\n• Cần cẩu nhấc một khối bê tông lên cao\n• Phanh xe sinh công cản âm để dừng xe",
                "Kéo hòm gỗ đi xa 10m bằng lực F = 50N hướng chếch 60°:\nA = F · s · cosα = 50 · 10 · cos(60°) = 250 J",
                "Theo chương trình GDPT 2018, s là độ dịch chuyển của điểm đặt lực."),
            ["Công suất"] = new("• P: công suất thực hiện (W)\n• A: công cơ học sinh ra (J)\n• t: thời gian thực hiện công (s)\n• F: lực tác dụng (N)\n• v: tốc độ tức thời của vật (m/s)",
                "Công suất là đại lượng đặc trưng cho tốc độ thực hiện công của lực, đo bằng công thực hiện trong một đơn vị thời gian.",
                "• Đánh giá sức mạnh của động cơ ô tô (đo bằng Mã lực HP)\n• So sánh độ mạnh yếu của các loại máy bơm nước\n• Đo năng lượng tiêu thụ",
                "Một động cơ điện nâng tải và thực hiện công A = 6000 J trong thời gian 4 giây:\nP = A / t = 6000 / 4 = 1500 W = 1.5 kW",
                "Một mã lực (1 HP) bằng khoảng 746 W."),
            ["Định luật Ohm"] = new("• I: cường độ dòng điện trong đoạn mạch (A)\n• U: hiệu điện thế đặt vào hai đầu đoạn mạch (V)\n• R: điện trở của đoạn mạch (Ω)",
                "Cường độ dòng điện chạy qua dây dẫn luôn tỉ lệ thuận với hiệu điện thế đặt vào hai đầu dây và tỉ lệ nghịch với điện trở của dây dẫn đó.",
                "• Thiết kế bảng mạch điện tử đơn giản\n• Điều chỉnh độ sáng bóng đèn bằng biến trở\n• Lựa chọn adapter nguồn phù hợp cho điện thoại",
                "Mắc điện trở R = 10 Ω vào nguồn điện có U = 12 V:\nI = U / R = 12 / 10 = 1.2 A",
                "Chỉ đúng với vật dẫn kim loại ở nhiệt độ không đổi (vật dẫn tuân theo định luật Ohm)."),
            ["Điện trở"] = new("• R: điện trở của dây dẫn (Ω)\n• ρ: điện trở suất của chất liệu dây (Ω·m)\n• l: chiều dài của sợi dây dẫn (m)\n• S: diện tích tiết diện ngang của dây (m²)",
                "Điện trở của một dây dẫn hình trụ đồng chất phụ thuộc vào chiều dài dây, tiết diện dây và bản chất vật liệu làm dây.",
                "• Lựa chọn dây dẫn bằng đồng thay vì sắt để giảm hao phí truyền tải điện\n• Chế tạo cuộn dây sưởi ấm cho bình nóng lạnh\n• Thiết kế dây điện chịu dòng tải lớn",
                "Dây đồng có l = 50m, S = 2 mm² (2·10⁻⁶ m²), điện trở suất ρ = 1.7·10⁻⁸ Ω·m:\nR = ρ·l / S = 1.7·10⁻⁸ · 50 / (2·10⁻⁶) = 0.425 Ω",
                "Nhiệt độ môi trường tăng thường làm điện trở suất của kim loại tăng theo."),
            ["Công suất điện"] = new("• P: công suất tiêu thụ điện (W)\n• U: hiệu điện thế sử dụng (V)\n• I: cường độ dòng điện chạy qua (A)\n• R: điện trở của đoạn mạch thuần trở (Ω)",
                "Đo tốc độ tiêu thụ điện năng của thiết bị. Công thức tổng quát P = U·I áp dụng cho mọi thiết bị. Công thức P = I²·R = U²/R chỉ áp dụng cho đoạn mạch thuần trở.",
                "• Chọn công suất bóng đèn phù hợp với phòng ngủ/phòng khách\n• Tính toán phụ tải mạng điện gia đình tránh bị nhảy Aptomat\n• Tính tiền điện sinh hoạt",
                "Bóng đèn sợi đốt ghi 220V - 100W hoạt động bình thường:\nCường độ dòng điện I = P / U = 100 / 220 ≈ 0.45 A\nĐiện trở đèn R = U² / P = 220² / 100 = 484 Ω",
                "Bếp điện, bàn là, lò sưởi là các ví dụ điển hình của thiết bị thuần trở."),
            ["Điện năng tiêu thụ"] = new("• A: điện năng tiêu thụ (J hoặc Wh, kWh)\n• P: công suất điện của thiết bị (W)\n• t: thời gian thiết bị chạy liên tục (s hoặc h)\n• U: hiệu điện thế nguồn (V)\n• I: cường độ dòng điện hoạt động (A)",
                "Điện năng tiêu thụ của đoạn mạch bằng tích công suất tiêu thụ và thời gian hoạt động của đoạn mạch đó.",
                "• Đọc chỉ số trên công tơ điện hàng tháng\n• Tính toán chi phí năng lượng của tủ lạnh, điều hòa\n• Lập kế hoạch tiết kiệm điện",
                "Bình nóng lạnh công suất 2.5 kW hoạt động liên tục trong 2 giờ:\nA = P·t = 2.5 · 2 = 5 kWh (bằng 5 số điện)",
                "1 kWh (1 số điện) = 1000 W · 3600 giây = 3.6·10⁶ J."),
            ["Định luật Coulomb"] = new("• F: lực tương tác tĩnh điện giữa hai điện tích (N)\n• k: hằng số tĩnh điện = 9·10⁹ N·m²/C²\n• q₁, q₂: điện tích của hai hạt điện tích điểm (C)\n• r: khoảng cách giữa chúng (m)\n• ε: hằng số điện môi của môi trường chứa điện tích",
                "Lực tương tác tĩnh điện giữa hai điện tích điểm đặt trong điện môi đồng tính tỉ lệ thuận với tích độ lớn của hai điện tích đó và tỉ lệ nghịch với bình phương khoảng cách giữa chúng.",
                "• Giải thích lực liên kết giữa hạt nhân và đám mây electron\n• Ứng dụng trong máy lọc bụi tĩnh điện\n• Quy trình sơn tĩnh điện ô tô",
                "Đặt hai điện tích q₁ = 10⁻⁷ C và q₂ = −2·10⁻⁷ C cách nhau 10 cm (0.1 m) trong chân không (ε = 1):\nF = 9·10⁹ · |10⁻⁷ · (−2·10⁻⁷)| / 0.1² = 0.018 N",
                "Nếu lực F > 0 (các điện tích cùng dấu) thì đẩy nhau; F < 0 (trái dấu) thì hút nhau. Giá trị F tính toán về độ lớn luôn lấy trị tuyệt đối."),
            ["Bước sóng"] = new("• λ: bước sóng của sóng (m)\n• v: vận tốc truyền sóng (m/s)\n• f: tần số dao động của nguồn sóng (Hz)\n• T: chu kỳ sóng (s)",
                "Bước sóng là quãng đường mà sóng truyền đi được trong một chu kỳ dao động của nguồn sóng. Đây cũng là khoảng cách ngắn nhất giữa hai điểm dao động cùng pha trên cùng một phương truyền sóng.",
                "• Thiết kế ăng-ten bắt sóng FM, sóng điện thoại\n• Xác định màu sắc ánh sáng khả kiến (đỏ λ ≈ 700nm, tím λ ≈ 380nm)\n• Hoạt động của lò vi sóng",
                "Sóng âm có tần số f = 680 Hz truyền trong không khí với tốc độ v = 340 m/s:\nλ = v / f = 340 / 680 = 0.5 m = 50 cm",
                "Tốc độ truyền sóng v phụ thuộc vào bản chất môi trường (v_rắn > v_lỏng > v_khí)."),
            ["Tần số"] = new("• f: tần số dao động (Hz)\n• T: chu kỳ dao động (s)",
                "Tần số là số chu kỳ dao động toàn phần thực hiện được trong một đơn vị thời gian (1 giây). Tần số là đại lượng nghịch đảo của chu kỳ.",
                "• Xác định tần số của nguồn điện lưới quốc gia (50 Hz)\n• Tần số âm thanh tiếng nói con người\n• Tốc độ làm tươi của màn hình hiển thị (60Hz, 120Hz)",
                "Một con lắc lò xo dao động điều hòa thực hiện xong một dao động hết 0.2 giây (T = 0.2s):\nf = 1 / T = 1 / 0.2 = 5 Hz",
                "Đơn vị đo tần số là Hertz (Hz). 1 Hz = 1 dao động/giây."),
            ["Năng lượng photon"] = new("• E: năng lượng của một hạt photon ánh sáng (J)\n• h: hằng số Planck = 6.626·10⁻³⁴ J·s\n• f: tần số dao động của ánh sáng (Hz)\n• c: tốc độ ánh sáng trong chân không = 3·10⁸ m/s\n• λ: bước sóng ánh sáng (m)",
                "Thuyết lượng tử ánh sáng phát biểu rằng chùm sáng là chùm các hạt gọi là photon. Mỗi photon mang một năng lượng xác định E = hf không đổi khi truyền đi.",
                "• Hoạt động của tấm pin quang điện hấp thụ ánh sáng mặt trời\n• Máy chụp X-quang trong chẩn đoán y tế\n• Hệ thống laser cắt khắc công nghiệp",
                "Năng lượng photon của ánh sáng màu lục bước sóng λ = 500 nm (5·10⁻⁷ m):\nE = h·c / λ = (6.626·10⁻³⁴ · 3·10⁸) / (5·10⁻⁷) ≈ 3.98·10⁻¹⁹ J",
                "Photon chỉ tồn tại trong trạng thái chuyển động, không có photon đứng yên."),
            ["Einstein quang điện"] = new("• h: hằng số Planck = 6.626·10⁻³⁴ J·s\n• f: tần số ánh sáng kích thích (Hz)\n• A: công thoát electron của kim loại (J)\n• m: khối lượng của electron ≈ 9.1·10⁻³¹ kg\n• v_max: tốc độ ban đầu cực đại của quang electron bứt ra (m/s)",
                "Phương trình cân bằng năng lượng do Albert Einstein thiết lập cho hiện tượng quang điện ngoài. Năng lượng photon ánh sáng kích thích dùng để cung cấp công bứt electron khỏi liên kết và truyền động năng ban đầu.",
                "• Thiết kế mắt thần cửa tự động mở\n• Cảm biến ánh sáng trên camera\n• Các loại bóng bán dẫn nhạy sáng",
                "Chiếu chùm sáng có năng lượng photon h·f = 3.5 eV vào tấm kim loại có công thoát A = 2.0 eV:\n½·m·v²_max = h·f − A = 3.5 − 2.0 = 1.5 eV ≈ 2.4·10⁻¹⁹ J",
                "Hiện tượng quang điện chỉ xảy ra khi bước sóng kích thích λ ≤ λ₀ (giới hạn quang điện của kim loại)."),
            ["Nhiệt lượng"] = new("• Q: nhiệt lượng trao đổi (J)\n• m: khối lượng chất (kg)\n• c: nhiệt dung riêng của chất (J/kg·K)\n• Δt: độ biến thiên nhiệt độ của vật (°C hoặc K)",
                "Nhiệt lượng là phần nhiệt năng mà vật nhận thêm hay mất đi trong quá trình truyền nhiệt. Q > 0: vật thu nhiệt lượng, Q < 0: vật tỏa nhiệt lượng.",
                "• Tính lượng năng lượng cần để đun sôi bình nước ấm\n• Tính toán công suất hệ thống điều hòa làm mát phòng\n• Thiết kế lò sưởi",
                "Tính nhiệt lượng cần cung cấp để đun nóng 2 lít nước (m = 2 kg, c = 4180 J/kg·K) tăng từ 25°C lên 100°C (Δt = 75°C):\nQ = m·c·Δt = 2 · 4180 · 75 = 627,000 J = 627 kJ",
                "Nước có nhiệt dung riêng rất lớn (c ≈ 4180 J/kg·K) nên giữ nhiệt và làm mát rất tốt."),
            ["Phương trình trạng thái"] = new("• P: áp suất của khối khí (Pa, atm, bar)\n• V: thể tích của khối khí (m³, lít)\n• T: nhiệt độ tuyệt đối của khối khí (K = t°C + 273)",
                "Hệ thức Clapeyron mô tả trạng thái của một lượng khí lý tưởng xác định liên hệ qua ba thông số: Áp suất, Thể tích, và Nhiệt độ tuyệt đối.",
                "• Áp suất lốp xe máy tăng cao khi đi ngoài đường nắng nóng\n• Hoạt động nén giãn khí trong xi lanh động cơ đốt trong\n• Hiện tượng bóng bay bị vỡ khi thả bay lên cao gặp áp suất thấp",
                "Khối khí ở áp suất 1 atm, thể tích 3 lít, nhiệt độ 27°C (300K) bị nén xuống thể tích 1.5 lít ở nhiệt độ 127°C (400K):\nP₂ = P₁·V₁·T₂ / (V₂·T₁) = (1 · 3 · 400) / (1.5 · 300) = 2.67 atm",
                "Lưu ý bắt buộc phải đổi nhiệt độ Celsius sang Kelvin trước khi áp dụng công thức."),

            // ── HÓA HỌC ──
            ["Số mol"] = new("• n: số mol chất (mol)\n• m: khối lượng chất (g)\n• M: khối lượng mol của chất đó (g/mol)",
                "Mol là lượng chất chứa 6.022·10²³ hạt đơn vị nguyên tử hoặc phân tử (hằng số Avogadro). Công thức này tính số mol dựa vào khối lượng.",
                "• Định lượng hóa chất trước khi làm thí nghiệm hóa học\n• Quy đổi đơn vị trong sản xuất phân bón hóa học\n• Tính toán phản ứng",
                "Tính số mol nước có trong 36g H₂O (biết M = 18 g/mol):\nn = m / M = 36 / 18 = 2 mol",
                "Khối lượng mol M bằng nguyên tử khối hoặc phân tử khối của chất đó biểu diễn bằng đơn vị g/mol."),
            ["Số mol (thể tích)"] = new("• n: số mol chất khí (mol)\n• V: thể tích khí đo được (lít)",
                "Công thức tính nhanh số mol chất khí dựa vào thể tích ở các điều kiện tiêu chuẩn hoặc điều kiện chuẩn:\n- Ở điều kiện chuẩn mới (đkc: 25°C, 1 bar): n = V / 24.79 (áp dụng chương trình mới GDPT 2018).\n- Ở điều kiện tiêu chuẩn cũ (đktc: 0°C, 1 atm): n = V / 22.4.",
                "• Đo thể tích khí Oxi sinh ra để tính số mol trong thí nghiệm phân hủy thuốc tím\n• Tính thể tích khí CO₂ thoát ra khi cho đá vôi tác dụng với axit",
                "Tính số mol khí CO₂ chiếm thể tích V = 4.958 lít ở điều kiện chuẩn mới (đkc):\nn = V / 24.79 = 4.958 / 24.79 = 0.2 mol",
                "Chỉ áp dụng đối với các chất ở thể khí."),
            ["Khối lượng chất"] = new("• m: khối lượng thực tế của chất (g)\n• n: số mol chất (mol)\n• M: khối lượng mol của chất (g/mol)",
                "Công thức tính khối lượng chất từ số mol và khối lượng mol của chất đó. Khối lượng chất tỉ lệ thuận với số mol gửi vào phản ứng.",
                "• Cân chính xác số gam muối NaCl cần dùng để pha chế dung dịch\n• Xác định khối lượng kết tủa sau phản ứng",
                "Tính khối lượng của 0.15 mol CuSO₄ (khối lượng mol M = 160 g/mol):\nm = n · M = 0.15 · 160 = 24 g",
                "Cần xác định chính xác công thức hóa học để tính khối lượng mol M tương ứng."),
            ["Nồng độ mol"] = new("• C_M: nồng độ mol của dung dịch (mol/L hoặc M)\n• n: số mol chất tan (mol)\n• V: thể tích của dung dịch (lít)",
                "Nồng độ mol biểu thị số mol chất tan có trong một lít dung dịch. Là đơn vị nồng độ phổ biến nhất trong phòng thí nghiệm.",
                "• Pha chế dung dịch axit có nồng độ chính xác để tẩy rửa kim loại\n• Định lượng dung dịch chuẩn độ trong hóa phân tích\n• Pha nước muối",
                "Hòa tan 0.5 mol muối ăn NaCl vào nước thu được 2 lít dung dịch:\nC_M = n / V = 0.5 / 2 = 0.25 M",
                "Thể tích V phải là thể tích của cả dung dịch sau khi pha, tính bằng đơn vị lít."),
            ["Nồng độ %"] = new("• C%: nồng độ phần trăm (%)\n• m_ct: khối lượng của chất tan có trong dung dịch (g)\n• m_dd: tổng khối lượng của dung dịch (g)",
                "Nồng độ phần trăm biểu thị số gam chất tan có trong 100 gam dung dịch.",
                "• Sản xuất dung dịch cồn y tế sát trùng 70%\n• Dung dịch muối sinh lý NaCl 0.9% truyền tĩnh mạch\n• Pha chế giấm ăn 5%",
                "Hòa tan 15g đường vào 135g nước cất:\nKhối lượng dung dịch m_dd = 15 + 135 = 150g\nNồng độ phần trăm C% = (15 / 150) · 100% = 10%",
                "Khối lượng dung dịch m_dd bằng khối lượng chất tan cộng khối lượng dung môi."),
            ["Mối liên hệ CM và C%"] = new("• C_M: nồng độ mol của dung dịch (mol/L)\n• C%: nồng độ phần trăm (%)\n• D: khối lượng riêng của dung dịch (g/ml hoặc g/cm³)\n• M: khối lượng mol của chất tan (g/mol)",
                "Công thức chuyển đổi nhanh giữa nồng độ phần trăm và nồng độ mol của cùng một dung dịch mà không cần thông qua các bước tính trung gian dài dòng.",
                "• Quy đổi nhanh nồng độ axit sulfuric đậm đặc 98% mua từ nhà máy về nồng độ mol để pha loãng làm thí nghiệm",
                "Tính nồng độ mol dung dịch NaOH 10% biết khối lượng riêng D = 1.11 g/ml (M = 40 g/mol):\nC_M = (10 · D · C%) / M = (10 · 1.11 · 10) / 40 = 2.775 M",
                "Đơn vị khối lượng riêng D bắt buộc phải là g/ml để công thức số 10 có hiệu lực."),
            ["PV = nRT"] = new("• P: áp suất khí trong bình (bar hoặc atm)\n• V: thể tích khí (lít)\n• n: số mol khí (mol)\n• R: hằng số khí lý tưởng\n• T: nhiệt độ tuyệt đối (K)",
                "Phương trình trạng thái Mendeleev-Clapeyron P·V = n·R·T dùng để tính thông số chưa biết của chất khí lý tưởng. Việc chọn R phụ thuộc vào đơn vị áp suất:\n- Nếu P tính bằng bar: R = 0.08314 (lít·bar/mol·K) (chuẩn mới GDPT 2018).\n- Nếu P tính bằng atm: R = 0.08206 (lít·atm/mol·K) (chuẩn cũ).\n- Nếu dùng đơn vị SI (P là Pa, V là m³): R = 8.314 (J/mol·K).",
                "• Tính áp suất khí heli nạp vào khinh khí cầu\n• Tính lượng khí oxy còn lại trong bình dưỡng khí dựa trên đồng hồ áp suất",
                "Tính áp suất của 1 mol khí ở nhiệt độ 25°C (298.15 K) trong bình 24.79 lít (sử dụng R = 0.08314):\nP = n·R·T / V = (1 · 0.08314 · 298.15) / 24.79 ≈ 1 bar",
                "Nhiệt độ T luôn phải quy đổi về Kelvin (K = °C + 273.15)."),
            ["Entanpi"] = new("• ΔH: biến thiên entanpi của phản ứng hóa học (kJ)\n• ΣΔ_f H(sp): tổng nhiệt tạo thành của sản phẩm (kJ/mol)\n• ΣΔ_f H(tc): tổng nhiệt tạo thành của tác chất/chất tham gia (kJ/mol)",
                "Biến thiên entanpi chuẩn biểu thị lượng nhiệt tỏa ra hoặc thu vào trong quá trình phản ứng xảy ra ở áp suất không đổi:\n- ΔH < 0: Phản ứng tỏa nhiệt (hệ giải phóng nhiệt ra môi trường).\n- ΔH > 0: Phản ứng thu nhiệt (hệ nhận nhiệt từ môi trường).",
                "• Thiết kế túi chườm lạnh y tế tự phản ứng để giảm sưng (phản ứng thu nhiệt)\n• Đánh giá hiệu suất tỏa nhiệt của các loại xăng sinh học khi cháy",
                "Đốt cháy 1 mol metan tỏa ra nhiệt lượng cực lớn:\nCH₄ + 2O₂ → CO₂ + 2H₂O có ΔH = −890 kJ (phản ứng tỏa nhiệt mạnh)",
                "Nhiệt tạo thành của các đơn chất bền ở trạng thái chuẩn luôn bằng 0."),
            ["Hằng số cân bằng"] = new("• Kc: hằng số cân bằng theo nồng độ chất\n• [A], [B]...: nồng độ các chất ở trạng thái cân bằng (mol/L)\n• a, b...: các hệ số tỉ lượng tương ứng trong phương trình phản ứng",
                "Hằng số cân bằng Kc đặc trưng cho trạng thái cân bằng hóa học của phản ứng thuận nghịch ở một nhiệt độ xác định. Kc lớn chứng tỏ phản ứng thuận xảy ra tốt.",
                "• Tối ưu hóa hiệu suất tổng hợp khí amoniac NH₃ trong công nghiệp phân bón\n• Điều khiển nồng độ các chất trong bể mạ kim loại",
                "Xét phản ứng thuận nghịch: H₂(k) + I₂(k) ⇌ 2HI(k):\nHằng số Kc = [HI]² / ([H₂]·[I₂])",
                "Kc chỉ thay đổi khi nhiệt độ thay đổi. Nồng độ của các chất rắn nguyên chất không xuất hiện trong biểu thức Kc."),
            ["pH"] = new("• pH: chỉ số đo độ axit-bazơ của dung dịch\n• [H⁺]: nồng độ ion H⁺ có trong dung dịch (mol/L)",
                "pH biểu thị mức độ hoạt động của các ion H⁺ trong nước. Thang pH chuẩn từ 0 đến 14:\n- pH < 7: Dung dịch có môi trường axit.\n- pH = 7: Dung dịch trung tính (nước tinh khiết).\n- pH > 7: Dung dịch có môi trường bazơ.",
                "• Kiểm tra độ pH của nước hồ bơi để khử trùng an toàn\n• Kiểm tra chất lượng đất nông nghiệp để bón vôi cải tạo đất chua\n• Đo pH dạ dày",
                "Một dung dịch axit dịch vị dạ dày có [H⁺] = 10⁻² mol/L:\npH = −log[H⁺] = −log(10⁻²) = 2 (môi trường axit mạnh)",
                "Thang đo pH là thang logarit cơ số 10. pH giảm 1 đơn vị nghĩa là nồng độ H⁺ tăng lên 10 lần."),
            ["pOH"] = new("• pOH: chỉ số đo độ bazơ của dung dịch\n• [OH⁻]: nồng độ ion hydroxide trong dung dịch (mol/L)",
                "Đại lượng đo gián tiếp môi trường bazơ dựa trên nồng độ ion OH⁻. Thường dùng để tính nhanh pH cho các dung dịch kiềm mạnh.",
                "• Đánh giá mức độ ăn mòn của xà phòng, chất tẩy rửa cực mạnh\n• Kiểm định độ kiềm của nước thải công nghiệp trước khi xả ra sông ngòi",
                "Dung dịch nước vôi trong Ca(OH)₂ có [OH⁻] = 10⁻³ mol/L:\npOH = −log[OH⁻] = −log(10⁻³) = 3",
                "Ở nhiệt độ 25°C, ta luôn có tổng pH + pOH = 14. Vậy dung dịch trên có pH = 11."),
            ["Faraday"] = new("• m: khối lượng chất giải phóng ở điện cực (g)\n• A: khối lượng mol nguyên tử của chất thoát ra (g/mol)\n• I: cường độ dòng điện điện phân (A)\n• t: thời gian điện phân liên tục (s)\n• n: số electron trao đổi (hóa trị của ion kim loại)\n• F: hằng số Faraday ≈ 96500 C/mol (hoặc 96485 C/mol)",
                "Định luật Faraday tính khối lượng của một chất hóa học được giải phóng ra ở điện cực trong quá trình điện phân dung dịch hoặc điện phân nóng chảy.",
                "• Công nghệ mạ điện chống gỉ sét cho sắt thép (mạ crom, mạ kẽm)\n• Tinh chế đồng nguyên chất từ quặng mỏ thô điện hóa\n• Công nghệ đúc điện kim loại",
                "Điện phân dung dịch CuSO₄ với cường độ dòng điện 5A trong 1930 giây (A = 64, n = 2, F = 96500):\nm = (A·I·t) / (n·F) = (64 · 5 · 1930) / (2 · 96500) = 3.2g đồng",
                "Chú ý bắt buộc phải đổi thời gian điện phân t sang đơn vị giây (s) để tính toán đúng."),
            ["Tốc độ phản ứng"] = new("• v: tốc độ phản ứng trung bình\n• ΔC: lượng biến thiên nồng độ chất phản ứng hoặc sản phẩm (mol/L)\n• Δt: khoảng thời gian xảy ra sự biến thiên đó (s)",
                "Tốc độ phản ứng hóa học là đại lượng đặc trưng cho mức độ xảy ra nhanh hay chậm của phản ứng trong một khoảng thời gian xác định.",
                "• Sử dụng chất xúc tác men để lên men bia nhanh hơn\n• Bảo quản thực phẩm đông lạnh làm giảm tốc độ phản ứng ôi thiu phân hủy\n• Nghiền nhỏ đá vôi để nung nhanh hơn",
                "Trong phản ứng phân hủy chất A, nồng độ giảm từ 0.8 M xuống 0.5 M sau 15 giây:\nv = |0.5 − 0.8| / 15 = 0.02 mol/(L·s)",
                "Tốc độ phản ứng luôn có giá trị dương, nên ta lấy trị tuyệt đối của biến thiên nồng độ chất tham gia phản ứng (vì nồng độ giảm dần)."),

            // ── TOÁN HỌC ──
            ["PT bậc 2"] = new("• x: nghiệm số cần tìm\n• a, b, c: các hệ số thực của phương trình (a ≠ 0)\n• Δ: biệt thức delta = b² − 4ac",
                "Công thức nghiệm tổng quát giải phương trình ax² + bx + c = 0. Số nghiệm phụ thuộc dấu biệt thức:\n- Δ > 0: Phương trình có 2 nghiệm phân biệt.\n- Δ = 0: Phương trình có nghiệm kép x = −b / 2a.\n- Δ < 0: Phương trình vô nghiệm trên tập số thực.",
                "• Mô tả chuyển động bay của các vật thể ném (quỹ đạo parabol)\n• Tối ưu hóa doanh số bán hàng dạng đồ thị bậc hai\n• Thiết kế vòm cầu xây dựng",
                "Giải phương trình x² − 6x + 8 = 0 (a = 1, b = −6, c = 8):\nΔ = (−6)² − 4·1·8 = 36 − 32 = 4 (√Δ = 2)\nx₁ = (6 + 2) / 2 = 4; x₂ = (6 − 2) / 2 = 2",
                "Nếu hệ số b chẵn (b = 2b'), ta có thể sử dụng công thức thu gọn biệt thức Δ' = b'² − ac."),
            ["Vieta"] = new("• x₁, x₂: hai nghiệm số của phương trình bậc hai\n• a, b, c: các hệ số tương ứng của phương trình (a ≠ 0)",
                "Hệ thức Vieta thiết lập mối quan hệ đại số khăng khít giữa các nghiệm số của phương trình bậc hai và các hệ số số học của nó.",
                "• Nhẩm nhanh nghiệm phương trình không cần bấm máy tính\n• Xác định dấu của hai nghiệm (cùng dấu, trái dấu) phục vụ giải bất phương trình\n• Tìm hai số khi biết tổng và tích",
                "Tìm nghiệm của phương trình x² − 5x + 6 = 0:\nNhẩm tổng S = x₁ + x₂ = 5, tích P = x₁·x₂ = 6. Hai số thỏa mãn là 2 và 3.",
                "Hệ thức Vieta chỉ áp dụng được khi phương trình bậc hai đã có nghiệm thực (biệt thức Δ ≥ 0)."),
            ["Logarit tích"] = new("• a: cơ số của logarit (a > 0, a ≠ 1)\n• x, y: hai biểu thức/số thực mang giá trị dương",
                "Logarit của một tích bằng tổng các logarit thành phần của các thừa số.",
                "• Đưa phép nhân các số cực lớn về phép cộng đơn giản trong tính toán cổ điển\n• Phân tích thuật toán máy tính phức tạp",
                "Tính giá trị của log₂(8 · 16):\nlog₂(8 · 16) = log₂8 + log₂16 = 3 + 4 = 7",
                "Đảm bảo các thừa số x, y đều là số dương để logarit tồn tại nghĩa."),
            ["Logarit thương"] = new("• a: cơ số của logarit (a > 0, a ≠ 1)\n• x, y: hai biểu thức/số thực mang giá trị dương",
                "Logarit của một thương bằng hiệu các logarit thành phần của tử số và mẫu số.",
                "• Chuyển phép chia số lớn về phép trừ nhanh\n• Thang đo cường độ âm thanh trong cách âm xây dựng",
                "Tính giá trị của log₃(243 / 9):\nlog₃(243 / 9) = log₃243 − log₃9 = 5 − 2 = 3",
                "Đảm bảo cả x và y đều dương."),
            ["Đổi cơ số log"] = new("• a, b, c: các số thực dương làm cơ số, thỏa mãn a ≠ 1, c ≠ 1",
                "Hệ thức cho phép chuyển đổi cơ số của một logarit bất kỳ về cơ số mới thuận tiện hơn cho tính toán hoặc nhập liệu máy tính.",
                "• Bấm máy tính các logarit cơ số lạ khi máy chỉ hỗ trợ cơ số 10 (log) hoặc cơ số tự nhiên e (ln)\n• Lập trình thuật toán đồ họa toán học",
                "Tính giá trị log₈16 bằng cách đưa về cơ số 2:\nlog₈16 = log₂16 / log₂8 = 4 / 3 ≈ 1.33",
                "Một công thức rút gọn đặc biệt hữu ích: logₐb = 1 / log_b a (với b ≠ 1)."),
            ["Lãi suất kép"] = new("• A: số tiền nhận về cuối kỳ (cả gốc lẫn lãi)\n• P: số tiền gốc ban đầu gửi vào (hoặc đầu tư)\n• r: lãi suất mỗi kỳ (biểu diễn dưới dạng số thập phân)\n• n: tổng số kỳ tích lũy lãi",
                "Công thức tính tiền tích lũy tăng trưởng theo phương thức lãi nhập gốc (lãi mẹ đẻ lãi con), tiền lãi của kỳ này được cộng dồn làm gốc tính lãi cho kỳ tiếp theo.",
                "• Lập kế hoạch tài chính tiết kiệm dưỡng già lâu dài\n• Đánh giá hiệu suất sinh lời của quỹ mở, cổ phiếu dài hạn\n• Tính toán lãi vay ngân hàng trả góp",
                "Gửi tiết kiệm P = 100 triệu đồng, lãi suất r = 6%/năm (0.06) tích lũy trong n = 5 năm:\nA = P·(1 + r)ⁿ = 100 · (1.06)⁵ ≈ 133.82 triệu đồng",
                "Thời hạn kỳ hạn gửi và lãi suất r phải tương thích đồng nhất (ví dụ: lãi suất năm đi với số năm gửi)."),
            ["Pythagoras"] = new("• a, b: độ dài hai cạnh góc vuông của tam giác vuông\n• c: độ dài cạnh huyền (cạnh đối diện góc vuông)",
                "Trong một tam giác vuông, bình phương độ dài cạnh huyền bằng tổng bình phương độ dài của hai cạnh góc vuông.",
                "• Đo khoảng cách gián tiếp trên thực địa xây dựng (quy tắc vạch góc vuông 3-4-5 của thợ xây)\n• Tính toán khoảng cách chéo trong lập trình đồ họa và định vị GPS",
                "Tam giác vuông có hai cạnh góc vuông dài 3 cm và 4 cm:\nc² = a² + b² = 3² + 4² = 25 → c = √25 = 5 cm",
                "Định lý đảo Pythagoras giúp kiểm tra một tam giác có vuông hay không khi đã biết độ dài ba cạnh."),
            ["Diện tích tam giác"] = new("• S: diện tích bề mặt của hình tam giác\n• a: độ dài một cạnh làm cạnh đáy\n• h: chiều cao kẻ vuông góc từ đỉnh xuống cạnh đáy tương ứng",
                "Diện tích hình tam giác bằng một nửa tích của độ dài cạnh đáy nhân với chiều cao tương ứng của cạnh đáy đó.",
                "• Đo đạc diện tích thửa ruộng đất có hình dạng tam giác phẳng\n• Tính toán diện tích tôn lợp mái đầu hồi nhà hình tam giác",
                "Một tam giác có độ dài đáy a = 8 cm và chiều cao tương ứng h = 5 cm:\nS = ½·a·h = ½ · 8 · 5 = 20 cm²",
                "Chiều cao h và đáy a bắt buộc phải đo bằng cùng một đơn vị độ dài."),
            ["Diện tích tam giác đều"] = new("• S: diện tích tam giác đều\n• a: độ dài cạnh của tam giác đều\n• √3: hằng số căn bậc hai của 3 ≈ 1.732",
                "Công thức tính nhanh diện tích tam giác đều khi biết độ dài cạnh, được rút gọn từ công thức diện tích tổng quát bằng cách thế chiều cao h = a·√3 / 2.",
                "• Tính nhanh diện tích các bề mặt hình lăng trụ tam giác đều, hình chóp tam giác đều trong các bài toán thực tế\n• Tính toán nguyên vật liệu thiết kế hoa văn hình tam giác đều",
                "Tính diện tích tam giác đều có cạnh a = 4 cm:\nS = a²·√3 / 4 = 4²·√3 / 4 = 4√3 ≈ 6.93 cm²",
                "Chỉ áp dụng khi tam giác là tam giác đều (ba cạnh bằng nhau)."),
            ["Diện tích tam giác vuông"] = new("• S: diện tích tam giác vuông\n• a, b: độ dài hai cạnh góc vuông",
                "Diện tích tam giác vuông bằng một nửa tích hai cạnh góc vuông. Đây là trường hợp đặc biệt của công thức diện tích tam giác tổng quát khi một cạnh góc vuông đóng vai trò là chiều cao.",
                "• Thiết kế mái nhà dốc một phía dạng tam giác vuông\n• Phân chia lô đất góc đường có hình dạng tam giác vuông phẳng",
                "Một tam giác vuông có hai cạnh góc vuông dài 6 m và 8 m:\nS = ½·a·b = ½ · 6 · 8 = 24 m²",
                "Đảm bảo hai cạnh được chọn là hai cạnh kề góc vuông, không sử dụng cạnh huyền trong công thức này."),
            ["Diện tích hình tròn"] = new("• S: diện tích của hình tròn\n• r: bán kính hình tròn (khoảng cách từ tâm đến biên)\n• π: hằng số pi ≈ 3.14159",
                "Diện tích hình tròn bằng tích của hằng số Pi với bình phương bán kính của hình tròn đó.",
                "• Tính diện tích bề mặt bánh pizza để so sánh giá cả\n• Tính mặt cắt ngang của dây điện đồng để chịu tải điện\n• Thiết kế bồn hoa tròn",
                "Tính diện tích hình tròn có bán kính r = 10 cm:\nS = π·r² = 3.14159 · 10² ≈ 314.16 cm²",
                "Nếu tăng bán kính lên gấp 3 lần thì diện tích sẽ tăng vọt lên gấp 9 lần."),
            ["Chu vi hình tròn"] = new("• C: chu vi hình tròn (độ dài đường biên)\n• r: bán kính hình tròn\n• d: đường kính hình tròn (d = 2r)\n• π: hằng số pi ≈ 3.14159",
                "Chu vi hình tròn là độ dài đường biên bao quanh hình tròn, tỉ lệ thuận với đường kính theo hằng số Pi.",
                "• Tính chiều dài dây thép cần để quấn quanh viền bồn hoa hình tròn\n• Đo quãng đường đi của bánh xe tròn lăn một vòng",
                "Tính chu vi bánh xe đạp có bán kính r = 35 cm:\nC = 2·π·r = 2 · 3.14159 · 35 ≈ 219.91 cm",
                "Tỉ số giữa chu vi và đường kính của bất kỳ hình tròn nào luôn là hằng số π."),
            ["Thể tích hình cầu"] = new("• V: thể tích phần khối cầu (đơn vị thể tích)\n• r: bán kính của khối cầu\n• π: hằng số pi ≈ 3.14159",
                "Thể tích khối cầu giới hạn bởi mặt cầu có bán kính r, tỉ lệ với lũy thừa bậc ba của bán kính.",
                "• Tính lượng nước tối đa chứa trong bồn nước tạo dáng quả cầu\n• Tính dung tích vỏ bình gas quả cầu kim loại\n• Tính thể tích trái đất",
                "Tính thể tích quả bóng đá có bán kính r = 11 cm:\nV = 4/3·π·r³ = (4/3) · 3.14159 · 11³ ≈ 5575.28 cm³",
                "Bán kính tăng gấp đôi làm thể tích tăng mạnh gấp 8 lần."),
            ["Thể tích hình trụ"] = new("• V: thể tích của khối trụ tròn xoay\n• r: bán kính của mặt đáy tròn\n• h: chiều cao của hình trụ (khoảng cách giữa 2 đáy)",
                "Thể tích hình trụ bằng diện tích của mặt đáy tròn nhân với chiều cao thẳng đứng.",
                "• Tính dung tích chứa của một lon nước ngọt thông thường\n• Tính thể tích bê tông đúc cọc trụ xây cầu tròn",
                "Một lon bia có bán kính đáy r = 3 cm và chiều cao h = 12 cm:\nV = π·r²·h = 3.14159 · 3² · 12 ≈ 339.29 cm³",
                "Diện tích đáy trụ tròn là S_đáy = π·r²."),
            ["Thể tích hình nón"] = new("• V: thể tích của khối nón tròn xoay\n• r: bán kính đường tròn đáy\n• h: chiều cao hình nón (khoảng cách từ đỉnh đến mặt đáy)",
                "Thể tích của hình nón bằng một phần ba diện tích mặt đáy tròn nhân với chiều cao hình nón.",
                "• Tính lượng kem chứa bên trong vỏ ốc quế giòn\n• Tính thể tích khối cát xây dựng tự chảy tạo đống hình nón",
                "Một cái phễu lọc hình nón có bán kính đáy r = 5 cm, chiều cao h = 9 cm:\nV = π·r²·h / 3 = 3.14159 · 5² · 9 / 3 ≈ 235.62 cm³",
                "Thể tích khối nón bằng 1/3 thể tích khối trụ có cùng bán kính đáy và chiều cao."),
            ["Heron"] = new("• S: diện tích bề mặt tam giác\n• a, b, c: độ dài 3 cạnh của tam giác\n• p: nửa chu vi của tam giác = (a + b + c) / 2",
                "Công thức cổ điển Heron dùng để tính chính xác diện tích của một tam giác phẳng khi biết độ dài 3 cạnh, loại bỏ việc kẻ đường cao.",
                "• Đo đạc diện tích mảnh đất tam giác méo mó ngoài thực địa chỉ bằng thước dây đo 3 cạnh biên\n• Viết hàm tính diện tích tam giác trong code",
                "Tính diện tích tam giác có các cạnh a = 5m, b = 6m, c = 7m (nửa chu vi p = (5+6+7)/2 = 9m):\nS = √[9 · (9−5) · (9−6) · (9−7)] = √[9 · 4 · 3 · 2] = √216 ≈ 14.70 m²",
                "Độ dài ba cạnh nhập vào phải thỏa mãn hệ thức bất đẳng thức tam giác: tổng hai cạnh bất kỳ luôn lớn hơn cạnh còn lại."),
            ["Hệ thức cơ bản"] = new("• α: góc lượng giác bất kỳ (đơn vị radian hoặc độ)\n• sin α, cos α: giá trị lượng giác của góc α trên đường tròn",
                "Hệ thức lượng giác cơ bản cốt lõi nhất biểu diễn định lý Pythagoras trên đường tròn lượng giác có bán kính bằng 1.",
                "• Lập trình thuật toán quay các đối tượng 2D/3D trong game\n• Phân tích dao động điều hòa của sóng cơ và sóng điện từ",
                "Tính giá trị của sin²(45°) + cos²(45°):\nsin²(45°) + cos²(45°) = (√2/2)² + (√2/2)² = 0.5 + 0.5 = 1",
                "Đẳng thức lượng giác luôn đúng với mọi giá trị góc α thuộc tập số thực."),
            ["Công thức nhân đôi"] = new("• α: góc lượng giác bất kỳ\n• sin(2α), cos(2α): giá trị lượng giác của góc nhân đôi",
                "Công thức lượng giác dùng để biến đổi rút gọn biểu thức, biểu diễn các hàm số lượng giác của góc gấp đôi qua tích các hàm lượng giác của góc ban đầu.",
                "• Tính tầm xa vật ném xiên cực đại\n• Biến đổi các tích phân lượng giác phức tạp trong giải tích toán học",
                "Biết sin α = 0.6 và cos α = 0.8. Tính sin(2α):\nsin(2α) = 2·sinα·cosα = 2 · 0.6 · 0.8 = 0.96",
                "Công thức cos(2α) có 3 cách biểu diễn: cos²α − sin²α, 2·cos²α − 1, hoặc 1 − 2·sin²α."),
            ["Định lý sin"] = new("• a, b, c: độ dài 3 cạnh của tam giác\n• A, B, C: các góc đối diện tương ứng với các cạnh a, b, c\n• R: bán kính đường tròn ngoại tiếp tam giác đó",
                "Định lý sin khẳng định trong mọi tam giác, tỉ số giữa độ dài của mỗi cạnh và sin của góc đối diện là bằng nhau và bằng đường kính đường tròn ngoại tiếp.",
                "• Đo khoảng cách từ bờ đất ra một hòn đảo ngoài biển (phép đo tam giác đạc)\n• Xác định quỹ đạo bay của máy bay dựa trên tọa độ đài radar",
                "Tam giác có cạnh a = 6 cm đối diện góc A = 30°. Tính bán kính R:\n2R = a / sinA = 6 / sin(30°) = 6 / 0.5 = 12 cm → R = 6 cm",
                "Thường dùng khi đề bài cho biết một cạnh và hai góc kề, hoặc hai cạnh và một góc đối diện."),
            ["Định lý cos"] = new("• a, b, c: độ dài ba cạnh của tam giác\n• C: góc lượng giác xen giữa hai cạnh a và b",
                "Bình phương độ dài một cạnh tam giác bằng tổng bình phương độ dài hai cạnh còn lại trừ đi hai lần tích của chúng với cosin của góc xen giữa. Định lý cos là dạng tổng quát hóa của định lý Pythagoras.",
                "• Tính khoảng cách giữa hai tàu thủy khi biết hành trình đi lệch nhau góc C\n• Đo đạc thực địa xác định khoảng cách giữa 2 điểm bị chắn khuất không đi qua được",
                "Tam giác có cạnh a = 8 cm, b = 10 cm, góc xen giữa C = 60°:\nc² = a² + b² − 2·a·b·cos(60°) = 8² + 10² − 2·8·10·cos(60°) = 64 + 100 − 160·0.5 = 84 → c = √84 ≈ 9.17 cm",
                "Nếu góc C = 90° (tam giác vuông tại C), cos 90° = 0, công thức biến đổi thành Pythagoras c² = a² + b²."),
            ["Đạo hàm lũy thừa"] = new("• x: biến số toán học\n• n: số mũ lũy thừa (số thực bất kỳ)\n• (xⁿ)': đạo hàm theo biến x của biểu thức xⁿ",
                "Đạo hàm đo tốc độ thay đổi tức thời của hàm số tại một điểm. Đây là công thức nền tảng nhất của toán giải tích vi phân.",
                "• Xác định phương trình vận tốc tức thời từ phương trình chuyển động của quãng đường\n• Tìm tốc độ tăng trưởng doanh nghiệp tối ưu",
                "Tính đạo hàm của hàm số f(x) = x⁴ tại x = 2:\nf'(x) = 4·x³\nf'(2) = 4 · 2³ = 32",
                "Đạo hàm của hằng số tự do luôn bằng 0: (C)' = 0."),
            ["Đạo hàm tích"] = new("• u, v: hai hàm số biến x có đạo hàm tại điểm xét\n• u', v': đạo hàm tương ứng của u và v",
                "Công thức tính đạo hàm của tích hai hàm số: đạo hàm của tích bằng đạo hàm hàm thứ nhất nhân hàm thứ hai cộng hàm thứ nhất nhân đạo hàm hàm thứ hai.",
                "• Phân tích tốc độ thay đổi doanh thu (doanh thu = lượng bán · đơn giá, cả hai đều phụ thuộc thời gian)",
                "Tính đạo hàm của hàm số f(x) = x²·ln(x):\nf'(x) = (x²)'·ln(x) + x²·(ln(x))' = 2x·ln(x) + x²·(1/x) = 2x·ln(x) + x",
                "Mở rộng cho tích ba hàm số: (uvw)' = u'vw + uv'w + uvw'."),
            ["Đạo hàm thương"] = new("• u, v: hai hàm số theo biến x, điều kiện v(x) ≠ 0\n• u', v': đạo hàm của từng hàm u, v tương ứng",
                "Quy tắc tính đạo hàm của thương hai hàm số, đạo hàm của thương bằng đạo hàm tử nhân mẫu trừ tử nhân đạo hàm mẫu, chia cho bình phương mẫu số.",
                "• Phân tích tốc độ tăng trưởng kinh tế thu nhập bình quân (thu nhập quốc dân chia cho dân số)\n• Đánh giá hiệu quả sản xuất",
                "Tính đạo hàm của hàm số f(x) = (2x + 1) / (x − 1):\nf'(x) = [(2x+1)'·(x−1) − (2x+1)·(x−1)'] / (x−1)² = [2(x−1) − (2x+1)(1)] / (x−1)² = −3 / (x−1)²",
                "Lưu ý điều kiện mẫu số v(x) phải khác 0 trên miền khảo sát."),
            ["Tích phân cơ bản"] = new("• x: biến số thực hiện tích phân\n• n: số mũ lũy thừa của biến số, điều kiện n ≠ −1\n• C: hằng số tích phân tự do",
                "Công thức tính nguyên hàm/tích phân cơ bản nhất đối với hàm lũy thừa. Là phép toán nghịch đảo hoàn toàn của phép tính đạo hàm.",
                "• Tính toán diện tích hình phẳng giới hạn bởi đường cong parabol và trục hoành\n• Tính khối lượng phân bố không đều dọc thanh kim loại",
                "Tính tích phân bất định của f(x) = x³:\n∫x³dx = x⁴ / 4 + C",
                "Công thức không áp dụng được khi n = −1. Khi đó, ta có tích phân đặc biệt: ∫(1/x)dx = ln|x| + C."),
            ["Tích phân xác định"] = new("• a, b: hai giới hạn tích phân (cận dưới a và cận trên b)\n• f(x): hàm số liên tục trên đoạn giới hạn [a, b]\n• F(x): một nguyên hàm bất kỳ của hàm f(x) (F'(x) = f(x))",
                "Công thức Newton-Leibniz là nền tảng của giải tích toán học, tính hiệu số giá trị nguyên hàm tại hai cận biên để tìm giá trị tích phân xác định.",
                "• Tính thể tích khối tròn xoay trong các công trình kiến trúc mỹ thuật phức tạp\n• Tính công cơ học do lực biến đổi sinh ra\n• Xác định giá trị trung bình hiệu dụng dòng điện xoay chiều",
                "Tính tích phân xác định ∫₁³ 3x² dx (nguyên hàm là x³):\n∫₁³ 3x² dx = [x³]₁³ = 3³ − 1³ = 27 − 1 = 26",
                "Hàm số f(x) bắt buộc phải liên tục trên toàn bộ đoạn tích phân đóng [a, b].")
        };

        private void ShowFormulaDetail(string name, string formula, string desc, string grade, string subj, string? linkedToolKey = null)
        {
            if (detailOverlay == null || detailBody == null) return;

            // Increment stats on opening detail
            FormulaStatsManager.Increment(name);

            // Header
            if (detailTitle != null) detailTitle.Text = $"{name} — {grade}";
            if (detailFormula != null) detailFormula.Text = formula;

            // Subject color
            var colors = new Dictionary<string, (string C1, string C2)>
            {
                ["🔬 Vật lý"] = ("#1565C0", "#1976D2"),
                ["🧪 Hóa học"] = ("#2E7D32", "#43A047"),
                ["📐 Toán học"] = ("#E65100", "#F57C00"),
            };
            if (colors.TryGetValue(subj, out var c) && detailHeader != null)
            {
                var c1 = (Color)ColorConverter.ConvertFromString(c.C1);
                var c2 = (Color)ColorConverter.ConvertFromString(c.C2);
                detailHeader.Background = new LinearGradientBrush(c1, c2, 0);
            }

            // Body
            detailBody.Children.Clear();

            // Formula display
            AddCopyableSection(detailBody, "📐 Công thức (Click để copy)", formula, "#E3F2FD", 22, true);

            // Short desc
            AddSection(detailBody, "📝 Mô tả", desc, "#F5F5F5", 15, false);

            // Dựng hình minh họa vector nếu là hình học đặc biệt
            var visual = CreateGeometryVisual(name);
            if (visual != null)
            {
                detailBody.Children.Add(visual);
            }

            // Detailed info
            if (Details.TryGetValue(name, out var info))
            {
                AddSection(detailBody, "🔢 Các đại lượng", info.Variables, "#E8F5E9", 14, false);
                AddSection(detailBody, "💡 Ý nghĩa", info.Meaning, "#FFF3E0", 14, false);
                AddSection(detailBody, "🌍 Vận dụng thực tế", info.RealWorld, "#F3E5F5", 14, false);
                AddSection(detailBody, "📋 Ví dụ", info.Example, "#E0F7FA", 15, true);
                if (!string.IsNullOrEmpty(info.Note))
                    AddSection(detailBody, "📌 Lưu ý", info.Note, "#FFF8E1", 14, false);
            }
            else
            {
                AddSection(detailBody, "ℹ️ Thông tin", $"Công thức: {formula}\nMô tả: {desc}\nCấp: {grade}", "#F5F5F5", 14, false);
            }

            // Ghi chú Giáo viên
            AddTeacherNotesSection(detailBody, name);

            if (!string.IsNullOrEmpty(linkedToolKey))
            {
                var btnPractice = new Button
                {
                    Content = "⚡ Thực hành với Công cụ",
                    Height = 36, Margin = new Thickness(0, 10, 0, 0),
                    Background = new SolidColorBrush(Color.FromRgb(21, 101, 192)),
                    Foreground = Brushes.White, FontWeight = FontWeights.Bold,
                    Cursor = Cursors.Hand
                };
                btnPractice.Click += (s, ev) =>
                {
                    detailOverlay.Visibility = Visibility.Collapsed;
                    NavigateToToolKey(linkedToolKey);
                };
                detailBody.Children.Add(btnPractice);
            }

            detailOverlay.Visibility = Visibility.Visible;
        }

        private void NavigateToToolKey(string key)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(this);
            while (parent != null && parent is not LearningToolsHub)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            if (parent is LearningToolsHub hub)
            {
                hub.NavigateToTool(key);
            }
        }

        private static void AddCopyableSection(StackPanel parent, string title, string content, string bgHex, int fontSize, bool isMono)
        {
            var bgColor = (Color)ColorConverter.ConvertFromString(bgHex);
            var card = new Border
            {
                Background = new SolidColorBrush(bgColor),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 8),
                Cursor = Cursors.Hand,
                ToolTip = "Click để sao chép công thức"
            };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = title, FontSize = 12, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                Margin = new Thickness(0, 0, 0, 4)
            });
            sp.Children.Add(new TextBlock
            {
                Text = content, FontSize = fontSize,
                Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = fontSize * 1.4,
                FontFamily = Application.Current?.TryFindResource("InterFont") as FontFamily ?? new FontFamily("Segoe UI"),
                FontWeight = isMono ? FontWeights.SemiBold : FontWeights.Normal
            });

            string copyText = content;
            card.MouseLeftButtonDown += (s, e) =>
            {
                try
                {
                    Clipboard.SetText(copyText);
                    PlayCopySound();
                    card.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    card.BorderThickness = new Thickness(2);
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
                    timer.Tick += (_, _) =>
                    {
                        card.BorderBrush = null;
                        card.BorderThickness = new Thickness(0);
                        timer.Stop();
                    };
                    timer.Start();
                }
                catch { }
            };

            card.Child = sp;
            parent.Children.Add(card);
        }

        private static void AddSection(StackPanel parent, string title, string content, string bgHex, int fontSize, bool isMono)
        {
            var bgColor = (Color)ColorConverter.ConvertFromString(bgHex);
            var card = new Border
            {
                Background = new SolidColorBrush(bgColor),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 8)
            };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = title, FontSize = 12, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                Margin = new Thickness(0, 0, 0, 4)
            });

            TextBlock tb;
            if (title == "🔢 Các đại lượng")
            {
                tb = CreateItalicizedVariablesTextBlock(content, fontSize, new SolidColorBrush(Color.FromRgb(33, 33, 33)), fontSize * 1.4);
            }
            else
            {
                tb = new TextBlock
                {
                    Text = content, FontSize = fontSize,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = fontSize * 1.4,
                    FontFamily = Application.Current?.TryFindResource("InterFont") as FontFamily ?? new FontFamily("Segoe UI"),
                    FontWeight = isMono ? FontWeights.SemiBold : FontWeights.Normal
                };
            }
            sp.Children.Add(tb);
            card.Child = sp;
            parent.Children.Add(card);
        }

        private void CloseDetail_Click(object sender, RoutedEventArgs e)
        {
            if (detailOverlay != null) detailOverlay.Visibility = Visibility.Collapsed;
        }

        private void CloseDetail_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (detailOverlay != null) detailOverlay.Visibility = Visibility.Collapsed;
        }

        // ═══════════════════════════════════════════════════════════
        //  PHASE 4 ADDITIONS
        // ═══════════════════════════════════════════════════════════

        private static class FormulaFavoritesTracker
        {
            private static readonly string FavoritesFilePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "QASmartClass", "formula_favorites.json");

            private static readonly HashSet<string> Favorites = new();
            private static bool _isLoaded;

            private static void EnsureLoaded()
            {
                if (_isLoaded) return;
                _isLoaded = true;
                try
                {
                    if (System.IO.File.Exists(FavoritesFilePath))
                    {
                        string json = System.IO.File.ReadAllText(FavoritesFilePath);
                        var list = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
                        if (list != null)
                        {
                            foreach (var item in list) Favorites.Add(item);
                        }
                    }
                }
                catch { }
            }

            public static bool IsFavorite(string name)
            {
                EnsureLoaded();
                return Favorites.Contains(name);
            }

            public static bool ToggleFavorite(string name)
            {
                EnsureLoaded();
                bool added;
                if (Favorites.Contains(name))
                {
                    Favorites.Remove(name);
                    added = false;
                }
                else
                {
                    Favorites.Add(name);
                    added = true;
                }
                Save();
                return added;
            }

            public static List<string> GetFavorites()
            {
                EnsureLoaded();
                return Favorites.ToList();
            }

            private static void Save()
            {
                try
                {
                    string? dir = System.IO.Path.GetDirectoryName(FavoritesFilePath);
                    if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                    {
                        System.IO.Directory.CreateDirectory(dir);
                    }
                    string json = System.Text.Json.JsonSerializer.Serialize(Favorites.ToList());
                    System.IO.File.WriteAllText(FavoritesFilePath, json);
                }
                catch { }
            }
        }

        private static UIElement? CreateGeometryVisual(string name)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 250)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Height = 160,
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var grid = new Grid();
            var canvas = new Canvas
            {
                Width = 200, Height = 120,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            grid.Children.Add(canvas);
            border.Child = grid;

            var strokeBrush = new SolidColorBrush(Color.FromRgb(21, 101, 192));
            var fillBrush = new SolidColorBrush(Color.FromArgb(30, 33, 150, 243));

            if (name == "Diện tích tam giác vuông")
            {
                var poly = new System.Windows.Shapes.Polygon
                {
                    Stroke = strokeBrush, StrokeThickness = 2, Fill = fillBrush,
                    Points = new PointCollection(new Point[] { new Point(20, 20), new Point(20, 100), new Point(160, 100) })
                };
                canvas.Children.Add(poly);
                AddCanvasText(canvas, "a (chiều cao)", 2, 50, 10);
                AddCanvasText(canvas, "b (đáy)", 75, 103, 10);
                AddCanvasText(canvas, "∟", 21, 85, 12);
                return border;
            }
            else if (name == "Diện tích tam giác đều")
            {
                var poly = new System.Windows.Shapes.Polygon
                {
                    Stroke = strokeBrush, StrokeThickness = 2, Fill = fillBrush,
                    Points = new PointCollection(new Point[] { new Point(100, 20), new Point(40, 100), new Point(160, 100) })
                };
                canvas.Children.Add(poly);
                var line = new System.Windows.Shapes.Line
                {
                    Stroke = strokeBrush, StrokeThickness = 1, StrokeDashArray = new DoubleCollection(new double[] { 3, 3 }),
                    X1 = 100, Y1 = 20, X2 = 100, Y2 = 100
                };
                canvas.Children.Add(line);
                AddCanvasText(canvas, "a", 30, 60, 10);
                AddCanvasText(canvas, "a", 163, 60, 10);
                AddCanvasText(canvas, "a", 95, 103, 10);
                AddCanvasText(canvas, "h", 104, 50, 10);
                return border;
            }
            else if (name == "Diện tích hình tròn" || name == "Chu vi hình tròn")
            {
                var ellipse = new System.Windows.Shapes.Ellipse { Stroke = strokeBrush, StrokeThickness = 2, Fill = fillBrush, Width = 90, Height = 90 };
                Canvas.SetLeft(ellipse, 55); Canvas.SetTop(ellipse, 15); canvas.Children.Add(ellipse);
                var center = new System.Windows.Shapes.Ellipse { Fill = strokeBrush, Width = 4, Height = 4 };
                Canvas.SetLeft(center, 98); Canvas.SetTop(center, 58); canvas.Children.Add(center);
                var radLine = new System.Windows.Shapes.Line { Stroke = strokeBrush, StrokeThickness = 1.5, X1 = 100, Y1 = 60, X2 = 145, Y2 = 60 };
                canvas.Children.Add(radLine);
                AddCanvasText(canvas, "r (bán kính)", 105, 43, 9);
                return border;
            }
            else if (name == "Thể tích hình cầu")
            {
                var ellipse = new System.Windows.Shapes.Ellipse { Stroke = strokeBrush, StrokeThickness = 2, Fill = fillBrush, Width = 90, Height = 90 };
                Canvas.SetLeft(ellipse, 55); Canvas.SetTop(ellipse, 15); canvas.Children.Add(ellipse);
                var oval = new System.Windows.Shapes.Ellipse { Stroke = strokeBrush, StrokeThickness = 1, StrokeDashArray = new DoubleCollection(new double[] { 2, 2 }), Width = 90, Height = 30 };
                Canvas.SetLeft(oval, 55); Canvas.SetTop(oval, 45); canvas.Children.Add(oval);
                var center = new System.Windows.Shapes.Ellipse { Fill = strokeBrush, Width = 4, Height = 4 };
                Canvas.SetLeft(center, 98); Canvas.SetTop(center, 58); canvas.Children.Add(center);
                var radLine = new System.Windows.Shapes.Line { Stroke = strokeBrush, StrokeThickness = 1.5, X1 = 100, Y1 = 60, X2 = 145, Y2 = 60 };
                canvas.Children.Add(radLine);
                AddCanvasText(canvas, "r", 120, 43, 10);
                return border;
            }
            else if (name == "Thể tích hình trụ")
            {
                var path = new System.Windows.Shapes.Path { Stroke = strokeBrush, StrokeThickness = 2, Fill = fillBrush, Data = Geometry.Parse("M 60,30 L 60,90 A 40,15 0 0,0 140,90 L 140,30 Z") };
                canvas.Children.Add(path);
                var topEll = new System.Windows.Shapes.Ellipse { Stroke = strokeBrush, StrokeThickness = 2, Fill = new SolidColorBrush(Color.FromArgb(40, 33, 150, 243)), Width = 80, Height = 30 };
                Canvas.SetLeft(topEll, 60); Canvas.SetTop(topEll, 15); canvas.Children.Add(topEll);
                var center = new System.Windows.Shapes.Ellipse { Fill = strokeBrush, Width = 4, Height = 4 };
                Canvas.SetLeft(center, 98); Canvas.SetTop(center, 28); canvas.Children.Add(center);
                var rad = new System.Windows.Shapes.Line { Stroke = strokeBrush, StrokeThickness = 1, X1 = 100, Y1 = 30, X2 = 140, Y2 = 30 };
                canvas.Children.Add(rad);
                AddCanvasText(canvas, "r", 118, 13, 9);
                AddCanvasText(canvas, "h (chiều cao)", 144, 50, 10);
                return border;
            }
            else if (name == "Thể tích hình nón")
            {
                var poly = new System.Windows.Shapes.Polygon { Stroke = strokeBrush, StrokeThickness = 2, Fill = fillBrush, Points = new PointCollection(new Point[] { new Point(100, 20), new Point(60, 95), new Point(140, 95) }) };
                canvas.Children.Add(poly);
                var bottomEll = new System.Windows.Shapes.Ellipse { Stroke = strokeBrush, StrokeThickness = 1.5, Fill = fillBrush, Width = 80, Height = 20 };
                Canvas.SetLeft(bottomEll, 60); Canvas.SetTop(bottomEll, 85); canvas.Children.Add(bottomEll);
                var center = new System.Windows.Shapes.Ellipse { Fill = strokeBrush, Width = 4, Height = 4 };
                Canvas.SetLeft(center, 98); Canvas.SetTop(center, 93); canvas.Children.Add(center);
                var line = new System.Windows.Shapes.Line { Stroke = strokeBrush, StrokeThickness = 1, StrokeDashArray = new DoubleCollection(new double[] { 3, 3 }), X1 = 100, Y1 = 20, X2 = 100, Y2 = 95 };
                canvas.Children.Add(line);
                var rad = new System.Windows.Shapes.Line { Stroke = strokeBrush, StrokeThickness = 1, X1 = 100, Y1 = 95, X2 = 140, Y2 = 95 };
                canvas.Children.Add(rad);
                AddCanvasText(canvas, "r", 118, 77, 9);
                AddCanvasText(canvas, "h", 88, 50, 10);
                return border;
            }
            return null;
        }

        private static void AddCanvasText(Canvas canvas, string text, double left, double top, int fontSize)
        {
            var tb = new TextBlock
            {
                Text = text, FontSize = fontSize, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66))
            };
            Canvas.SetLeft(tb, left); Canvas.SetTop(tb, top); canvas.Children.Add(tb);
        }

        private static TextBlock CreateItalicizedVariablesTextBlock(string text, int fontSize, Brush foreground, double lineHeight)
        {
            var tb = new TextBlock
            {
                FontSize = fontSize,
                Foreground = foreground,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = lineHeight,
                FontFamily = Application.Current?.TryFindResource("InterFont") as FontFamily ?? new FontFamily("Segoe UI")
            };

            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.StartsWith("• ") && line.Contains(":"))
                {
                    tb.Inlines.Add(new System.Windows.Documents.Run("• ") { FontStyle = FontStyles.Normal });
                    
                    int colonIdx = line.IndexOf(':');
                    string varPart = line.Substring(2, colonIdx - 2);
                    string descPart = line.Substring(colonIdx);

                    tb.Inlines.Add(new System.Windows.Documents.Run(varPart) { FontStyle = FontStyles.Italic, FontWeight = FontWeights.SemiBold });
                    tb.Inlines.Add(new System.Windows.Documents.Run(descPart) { FontStyle = FontStyles.Normal });
                }
                else
                {
                    tb.Inlines.Add(new System.Windows.Documents.Run(line) { FontStyle = FontStyles.Normal });
                }

                if (i < lines.Length - 1)
                {
                    tb.Inlines.Add(new System.Windows.Documents.LineBreak());
                }
            }

            return tb;
        }

        private static void PlayCopySound()
        {
            try
            {
                System.Media.SystemSounds.Asterisk.Play();
            }
            catch { }
        }

        private static class FormulaTeacherNotesManager
        {
            private static readonly string NotesFilePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "QASmartClass", "formula_teacher_notes.json");

            private static readonly Dictionary<string, string> Notes = new();
            private static bool _isLoaded;

            private static void EnsureLoaded()
            {
                if (_isLoaded) return;
                _isLoaded = true;
                try
                {
                    if (System.IO.File.Exists(NotesFilePath))
                    {
                        string json = System.IO.File.ReadAllText(NotesFilePath);
                        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                        if (dict != null)
                        {
                            foreach (var kvp in dict) Notes[kvp.Key] = kvp.Value;
                        }
                    }
                    else
                    {
                        // Khởi tạo ghi chú mẫu chất lượng cao cho giáo viên dễ hình dung
                        Notes["Tốc độ (Vận tốc thẳng)"] = "💡 Hướng dẫn giảng dạy:\n- Nhấn mạnh cho học sinh sự khác biệt giữa Tốc độ (vô hướng, quãng đường/thời gian) và Vận tốc (vectơ, độ dịch chuyển/thời gian) theo chương trình GDPT 2018.\n- Ví dụ thực tế: Đồng hồ tốc kế trên xe máy hiển thị tốc độ tức thời, không hiển thị hướng nên không phải vận tốc.";
                        Notes["Định luật Coulomb"] = "💡 Mẹo sư phạm tránh bẫy:\n- Nhắc nhở học sinh luôn dùng trị tuyệt đối cho tích điện tích |q₁·q₂| vì lực tĩnh điện luôn có độ lớn không âm.\n- Lưu ý hằng số điện môi ε của chân không hoặc không khí bằng 1, các môi trường khác lớn hơn 1 làm giảm lực tương tác.";
                        Notes["Số mol (thể tích)"] = "💡 Lưu ý GDPT 2018:\n- Tránh nhầm lẫn với điều kiện tiêu chuẩn cũ (22.4 lít ở 0°C, 1 atm).\n- Tiêu chuẩn mới dùng điều kiện chuẩn (đkc: 25°C, 1 bar) với hằng số thể tích mol là 24.79 lít.";
                        Notes["Diện tích tam giác đều"] = "💡 Công thức tính nhanh nâng cao:\n- Giúp học sinh lớp 9 giải nhanh các bài toán hình học không gian (tính diện tích đáy của hình chóp tam giác đều).\n- Chứng minh nhanh bằng công thức Heron hoặc hạ đường cao h = a·√3 / 2.";
                        
                        try
                        {
                            string? dir = System.IO.Path.GetDirectoryName(NotesFilePath);
                            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                            {
                                System.IO.Directory.CreateDirectory(dir);
                            }
                            string json = System.Text.Json.JsonSerializer.Serialize(Notes);
                            System.IO.File.WriteAllText(NotesFilePath, json);
                        }
                        catch { }
                    }
                }
                catch { }
            }

            public static string GetNote(string formulaName)
            {
                EnsureLoaded();
                return Notes.TryGetValue(formulaName, out var note) ? note : "";
            }

            public static void SaveNote(string formulaName, string note)
            {
                EnsureLoaded();
                Notes[formulaName] = note;
                try
                {
                    string? dir = System.IO.Path.GetDirectoryName(NotesFilePath);
                    if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                    {
                        System.IO.Directory.CreateDirectory(dir);
                    }
                    string json = System.Text.Json.JsonSerializer.Serialize(Notes);
                    System.IO.File.WriteAllText(NotesFilePath, json);
                }
                catch { }
            }
        }

        private static class FormulaStatsManager
        {
            private static readonly string StatsFilePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "QASmartClass", "formula_stats.json");

            private static readonly Dictionary<string, int> Stats = new();
            private static bool _isLoaded;

            private static void EnsureLoaded()
            {
                if (_isLoaded) return;
                _isLoaded = true;
                try
                {
                    if (System.IO.File.Exists(StatsFilePath))
                    {
                        string json = System.IO.File.ReadAllText(StatsFilePath);
                        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(json);
                        if (dict != null)
                        {
                            foreach (var kvp in dict) Stats[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch { }
            }

            public static int GetCount(string name)
            {
                EnsureLoaded();
                return Stats.TryGetValue(name, out var count) ? count : 0;
            }

            public static void Increment(string name)
            {
                EnsureLoaded();
                if (Stats.ContainsKey(name)) Stats[name]++;
                else Stats[name] = 1;
                Save();
            }

            private static void Save()
            {
                try
                {
                    string? dir = System.IO.Path.GetDirectoryName(StatsFilePath);
                    if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                    {
                        System.IO.Directory.CreateDirectory(dir);
                    }
                    string json = System.Text.Json.JsonSerializer.Serialize(Stats);
                    System.IO.File.WriteAllText(StatsFilePath, json);
                }
                catch { }
            }
        }

        private static void AddTeacherNotesSection(StackPanel parent, string name)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(240, 244, 248)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(209, 219, 229)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = "👨‍🏫 Ghi chú giảng dạy của Giáo viên (Lưu cục bộ)",
                FontSize = 12, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                Margin = new Thickness(0, 0, 0, 6)
            });

            string currentNote = FormulaTeacherNotesManager.GetNote(name);

            var txtNote = new TextBox
            {
                Text = currentNote,
                FontSize = 13,
                MinLines = 2, MaxLines = 5,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 6),
                Padding = new Thickness(4)
            };
            sp.Children.Add(txtNote);

            var btnSave = new Button
            {
                Content = "💾 Lưu ghi chú",
                HorizontalAlignment = HorizontalAlignment.Right,
                Padding = new Thickness(12, 4, 12, 4),
                Background = new SolidColorBrush(Color.FromRgb(15, 118, 110)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand
            };
            btnSave.Click += (s, e) =>
            {
                FormulaTeacherNotesManager.SaveNote(name, txtNote.Text);
                btnSave.Content = "✓ Đã lưu thành công";
                btnSave.Background = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
                timer.Tick += (_, _) =>
                {
                    btnSave.Content = "💾 Lưu ghi chú";
                    btnSave.Background = new SolidColorBrush(Color.FromRgb(15, 118, 110));
                    timer.Stop();
                };
                timer.Start();
            };
            sp.Children.Add(btnSave);

            card.Child = sp;
            parent.Children.Add(card);
        }

        private void StartQuiz_Click(object sender, RoutedEventArgs e)
        {
            _quizCurrentQuestionIndex = 0;
            _quizScore = 0;
            if (quizOverlay == null) return;
            quizOverlay.Visibility = Visibility.Visible;
            GenerateQuestion();
        }

        private void CloseQuiz_Click(object sender, RoutedEventArgs e)
        {
            if (quizOverlay != null) quizOverlay.Visibility = Visibility.Collapsed;
        }

        private void CloseQuiz_Click(object sender, MouseButtonEventArgs e)
        {
            if (quizOverlay != null) quizOverlay.Visibility = Visibility.Collapsed;
        }

        private void NextQuestion_Click(object sender, RoutedEventArgs e)
        {
            if (_quizCurrentQuestionIndex >= QuizTotalQuestions)
            {
                if (btnNextQuestion != null && btnNextQuestion.Content.ToString() == "🔄 Bắt đầu lượt chơi mới")
                {
                    StartQuiz_Click(sender, e);
                }
                else
                {
                    ShowQuizSummary();
                }
            }
            else
            {
                GenerateQuestion();
            }
        }

        private void ShowQuizSummary()
        {
            if (quizQuestion == null || quizOptions == null || quizFeedback == null || btnNextQuestion == null || quizStatsHeader == null) return;

            quizQuestion.Text = $"🎉 Hoàn thành thử thách ôn luyện!\n\nSố điểm của bạn: {_quizScore} / {QuizTotalQuestions * 10} điểm.";
            quizOptions.Children.Clear();
            quizFeedback.Text = "";
            quizStatsHeader.Text = "Hoàn thành!";
            
            btnNextQuestion.Content = "🔄 Bắt đầu lượt chơi mới";
            btnNextQuestion.Visibility = Visibility.Visible;
        }

        private void GenerateQuestion()
        {
            if (quizQuestion == null || quizOptions == null || quizFeedback == null || btnNextQuestion == null || quizStatsHeader == null) return;

            _quizCurrentQuestionIndex++;
            quizStatsHeader.Text = $"Câu hỏi: {_quizCurrentQuestionIndex}/{QuizTotalQuestions} • Điểm số: {_quizScore}";

            quizFeedback.Text = "";
            btnNextQuestion.Visibility = Visibility.Collapsed;
            quizOptions.Children.Clear();

            var favList = FormulaFavoritesTracker.GetFavorites();
            List<(string Name, string Formula, string Subj)> formulas = new();

            foreach (var kvp in AllFormulas)
            {
                string subj = kvp.Key;
                if (_activeSubject == "Tất cả" || _activeSubject == subj)
                {
                    foreach (var item in kvp.Value)
                    {
                        formulas.Add((item.Name, item.Formula, subj));
                    }
                }
                else if (_activeSubject == "⭐ Yêu thích")
                {
                    foreach (var item in kvp.Value)
                    {
                        if (favList.Contains(item.Name))
                        {
                            formulas.Add((item.Name, item.Formula, subj));
                        }
                    }
                }
            }

            if (formulas.Count < 3)
            {
                formulas.Clear();
                foreach (var kvp in AllFormulas)
                {
                    foreach (var item in kvp.Value)
                    {
                        formulas.Add((item.Name, item.Formula, kvp.Key));
                    }
                }
            }

            if (formulas.Count < 3) return;

            var rand = new Random();
            var correctItem = formulas[rand.Next(formulas.Count)];

            var otherItems = formulas.Where(x => x.Name != correctItem.Name).OrderBy(x => rand.Next()).Take(2).ToList();
            if (otherItems.Count < 2) return;

            var optionsList = new List<(string Text, bool IsCorrect)>
            {
                (correctItem.Formula, true),
                (otherItems[0].Formula, false),
                (otherItems[1].Formula, false)
            };

            optionsList = optionsList.OrderBy(x => rand.Next()).ToList();

            quizQuestion.Text = $"Câu hỏi: Công thức nào sau đây là công thức của đại lượng \n👉 \"{correctItem.Name}\"?";

            foreach (var opt in optionsList)
            {
                var btn = new Button
                {
                    Content = opt.Text,
                    FontSize = 15,
                    FontWeight = FontWeights.SemiBold,
                    Height = 44,
                    Margin = new Thickness(0, 0, 0, 8),
                    Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    BorderThickness = new Thickness(1.5),
                    Cursor = Cursors.Hand,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center
                };

                btn.Resources.Add(typeof(Border), new Style(typeof(Border))
                {
                    Setters = { new Setter(Border.CornerRadiusProperty, new CornerRadius(8)) }
                });

                bool optIsCorrect = opt.IsCorrect;
                btn.Click += (s, e) =>
                {
                    foreach (UIElement child in quizOptions.Children)
                    {
                        if (child is Button b) b.IsEnabled = false;
                    }

                    if (optIsCorrect)
                    {
                        _quizScore += 10;
                        quizStatsHeader.Text = $"Câu hỏi: {_quizCurrentQuestionIndex}/{QuizTotalQuestions} • Điểm số: {_quizScore}";

                        btn.Background = new SolidColorBrush(Color.FromRgb(220, 252, 231));
                        btn.BorderBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                        btn.Foreground = new SolidColorBrush(Color.FromRgb(21, 128, 61));
                        quizFeedback.Text = "🎉 Chính xác! Bạn trả lời rất tốt.";
                        quizFeedback.Foreground = new SolidColorBrush(Color.FromRgb(21, 128, 61));
                        PlayCopySound();
                    }
                    else
                    {
                        btn.Background = new SolidColorBrush(Color.FromRgb(254, 226, 226));
                        btn.BorderBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                        btn.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
                        quizFeedback.Text = $"❌ Chưa đúng rồi! Công thức đúng là: {correctItem.Formula}";
                        quizFeedback.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));

                        foreach (UIElement child in quizOptions.Children)
                        {
                            if (child is Button b && b.Content.ToString() == correctItem.Formula)
                            {
                                b.Background = new SolidColorBrush(Color.FromRgb(220, 252, 231));
                                b.BorderBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                                b.Foreground = new SolidColorBrush(Color.FromRgb(21, 128, 61));
                            }
                        }
                    }

                    if (_quizCurrentQuestionIndex >= QuizTotalQuestions)
                    {
                        btnNextQuestion.Content = "📊 Xem kết quả chung cuộc";
                    }
                    else
                    {
                        btnNextQuestion.Content = "Câu hỏi tiếp theo ➔";
                    }
                    btnNextQuestion.Visibility = Visibility.Visible;
                };

                quizOptions.Children.Add(btn);
            }
        }

        private void LoadPracticalApps()
        {
            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            string suffix = isVN ? "VN" : "EN";

            var items = new System.Collections.Generic.List<PracticalAppItem>
            {
                new PracticalAppItem
                {
                    Icon = "🏗️",
                    Title = isVN ? "Kỹ thuật Xây dựng & Tính tải trọng" : "Structural Engineering & Load Calculations",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_formulas_1_{suffix}.png",
                    Description = isVN 
                        ? "Các kỹ sư xây dựng sử dụng các công thức toán học và vật lý (như Pythagoras, Heron, lực thế năng, mô-men lực) để tính toán tải trọng, độ bền vật liệu và thiết kế kết cấu cầu đường, nhà ở đảm bảo an toàn chịu lực." 
                        : "Civil engineers use mathematical and physical formulas (such as Pythagoras, Heron, potential energy, torque) to calculate load capacities, material strength, and design bridges and buildings to ensure structural safety."
                },
                new PracticalAppItem
                {
                    Icon = "🧪",
                    Title = isVN ? "Sản xuất Hóa chất & Phân tích Hiệu suất" : "Chemical Manufacturing & Yield Analysis",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_formulas_2_{suffix}.png",
                    Description = isVN 
                        ? "Trong công nghiệp hóa chất, việc tính số mol, nồng độ dung dịch (CM, C%), và hiệu suất phản ứng giúp kiểm soát tỷ lệ nguyên liệu đầu vào và sản lượng đầu ra, tối ưu hóa quy trình sản xuất đại trà." 
                        : "In the chemical industry, calculating moles, solution concentrations (CM, C%), and reaction yields helps control raw material ratios and output volumes, optimizing large-scale manufacturing processes."
                },
                new PracticalAppItem
                {
                    Icon = "💻",
                    Title = isVN ? "Đồ họa Máy tính & Mô phỏng Vật lý" : "Computer Graphics & Physics Simulations",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_formulas_3_{suffix}.png",
                    Description = isVN 
                        ? "Các nhà lập trình trò chơi và đồ họa 3D áp dụng các công thức động học (vận tốc, gia tốc, trọng lực, rơi tự do) để mô phỏng chuyển động thực tế của nhân vật, nước, lửa, và hiệu ứng ánh sáng trong môi trường ảo." 
                        : "Game developers and 3D graphics programmers apply kinematics formulas (velocity, acceleration, gravity, free fall) to simulate realistic movements of characters, water, fire, and lighting effects in virtual environments."
                }
            };

            try
            {
                if (practicalAppViewer != null) practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for FormulasTool: {Err}", ex.Message);
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