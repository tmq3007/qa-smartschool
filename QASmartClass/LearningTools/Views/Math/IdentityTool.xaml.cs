using System;
using QASmartClass.LearningTools.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Models;

namespace QASmartClass.LearningTools.Views.Math
{
    /// <summary>
    /// 7 Hằng Đẳng Thức Đáng Nhớ — bảng tra cứu, kiểm chứng bằng số, mẹo nhớ
    /// </summary>
    public partial class IdentityTool : BaseToolControl
    {
        private static readonly (string Num, string Name, string Formula, string Expansion, string BgHex, string FgHex)[] Identities =
        {
            ("1", "Bình phương của tổng",   "(A + B)²", "A² + 2AB + B²",                   "#E3F2FD", "#1565C0"),
            ("2", "Bình phương của hiệu",   "(A − B)²", "A² − 2AB + B²",                   "#F3E5F5", "#7B1FA2"),
            ("3", "Hiệu hai bình phương",   "A² − B²",  "(A + B)(A − B)",                   "#E8F5E9", "#2E7D32"),
            ("4", "Lập phương của tổng",    "(A + B)³", "A³ + 3A²B + 3AB² + B³",            "#FFF3E0", "#E65100"),
            ("5", "Lập phương của hiệu",    "(A − B)³", "A³ − 3A²B + 3AB² − B³",            "#FCE4EC", "#C62828"),
            ("6", "Tổng hai lập phương",    "A³ + B³",  "(A + B)(A² − AB + B²)",             "#E0F2F1", "#00695C"),
            ("7", "Hiệu hai lập phương",    "A³ − B³",  "(A − B)(A² + AB + B²)",             "#E8EAF6", "#283593"),
        };

        private bool _verifyBuilt, _tipsBuilt, _geoBuilt;
        private int _selectedGeoIdentity = 0;
        private double _lastVerifyWidth = 0;
        private double _lastTipsWidth = 0;
        private bool _isDrawing = false;

        // Thuộc tính hỗ trợ tương tác đồ hoạ 3D
        private double _angleX = 25;
        private double _angleY = -35;
        private Point _lastMousePosition;
        private bool _isMouseCaptured;
        private System.Windows.Media.Media3D.Model3DGroup _modelGroup;

        private void LocalizeSidebar()
        {
            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
            if (menuTextTable != null) menuTextTable.Text = isVN ? "Bảng hằng đẳng thức" : "Identities Chart";
            if (menuTextVerify != null) menuTextVerify.Text = isVN ? "Khảo sát & Tính toán" : "Verify & Calculate";
            if (menuTextGeo != null) menuTextGeo.Text = isVN ? "Minh họa hình học" : "Geometric View";
            if (menuTextTips != null) menuTextTips.Text = isVN ? "Mẹo ghi nhớ" : "Memory Tips";
            if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";
        }

        public IdentityTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                LocalizeSidebar();
                BuildIdentityTable();
                // Bàn phím số mini
                if (txtA != null) TouchNumPad.Attach(txtA, step: 1);
                if (txtB != null) TouchNumPad.Attach(txtB, step: 1);
                if (txtGeoA != null) TouchNumPad.Attach(txtGeoA, step: 1, min: 1);
                if (txtGeoB != null) TouchNumPad.Attach(txtGeoB, step: 1, min: 1);

                // Apply graphics quality settings
                ApplyGraphicsSettings();

                // Đăng ký sự kiện đổi ngôn ngữ động
                QASmartClass.Shared.LanguageManager.LanguageChanged -= OnLanguageChanged;
                QASmartClass.Shared.LanguageManager.LanguageChanged += OnLanguageChanged;
            };

            Unloaded += (_, _) =>
            {
                // Giải phóng sự kiện tránh rò rỉ bộ nhớ
                QASmartClass.Shared.LanguageManager.LanguageChanged -= OnLanguageChanged;
            };
        }

        private void OnLanguageChanged(string langCode)
        {
            LocalizeSidebar();
            BuildIdentityTable();
            if (_verifyBuilt) RunVerification();
            if (_geoBuilt)
            {
                BuildGeoSelector();
                DrawGeometry();
            }
            if (_tipsBuilt) BuildTipsPanel();
            LoadPracticalApps();
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewTable == null || viewVerify == null || viewGeo == null || viewTips == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewTable.Visibility = Visibility.Collapsed;
            viewVerify.Visibility = Visibility.Collapsed;
            viewGeo.Visibility = Visibility.Collapsed;
            viewTips.Visibility = Visibility.Collapsed;
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
                    viewVerify.Visibility = Visibility.Visible;
                    _verifyBuilt = true;
                    RunVerification();
                    break;
                case 3:
                    viewGeo.Visibility = Visibility.Visible;
                    _geoBuilt = true;
                    BuildGeoSelector();
                    DrawGeometry();
                    break;
                case 4:
                    viewTips.Visibility = Visibility.Visible;
                    _tipsBuilt = true;
                    BuildTipsPanel();
                    break;
                case 5:
                    viewPractical.Visibility = Visibility.Visible;
                    LoadPracticalApps();
                    break;
            }
        }

        private void IdentityTool_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_verifyBuilt && verifyPanel != null)
            {
                double currentWidth = verifyPanel.ActualWidth;
                if (System.Math.Abs(currentWidth - _lastVerifyWidth) > 12)
                {
                    _lastVerifyWidth = currentWidth;
                    ResizeVerifyPanel();
                }
            }
            if (_tipsBuilt && tipsPanel != null)
            {
                double currentWidth = tipsPanel.ActualWidth;
                if (System.Math.Abs(currentWidth - _lastTipsWidth) > 12)
                {
                    _lastTipsWidth = currentWidth;
                    ResizeTipsPanel();
                }
            }
        }

        private void ResizeVerifyPanel()
        {
            if (verifyPanel == null) return;
            double pw = verifyPanel.ActualWidth > 10 ? verifyPanel.ActualWidth : 950;
            double cw = (pw / 2) - 10;
            if (cw < 350) cw = pw - 10;

            foreach (var child in verifyPanel.Children)
            {
                if (child is FrameworkElement fe)
                {
                    fe.Width = cw;
                }
            }
        }

        private void ResizeTipsPanel()
        {
            if (tipsPanel == null) return;
            double pw = tipsPanel.ActualWidth > 10 ? tipsPanel.ActualWidth : 950;
            double cw = (pw / 2) - 14;
            if (cw < 350) cw = pw - 14;

            foreach (var child in tipsPanel.Children)
            {
                if (child is FrameworkElement fe)
                {
                    fe.Width = cw;
                }
            }
        }

        private string GetStr(string key, string fallback)
        {
            try
            {
                return Application.Current.TryFindResource(key) as string ?? fallback;
            }
            catch
            {
                return fallback;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  TAB 1: BẢNG HĐT
        // ═══════════════════════════════════════════════════════════
        private void BuildIdentityTable()
        {
            identityPanel.Children.Clear();
            string badgeText = GetStr("Id_TabTable", "HĐT").Contains("Identities") ? "ID" : "HĐT";

            foreach (var (num, name, formula, expansion, bgHex, fgHex) in Identities)
            {
                string localizedName = GetStr($"Id_Name{num}", name);
                var bg = (Color)ColorConverter.ConvertFromString(bgHex);
                var fg = (Color)ColorConverter.ConvertFromString(fgHex);

                var card = new Border
                {
                    Background = Brushes.White, CornerRadius = new CornerRadius(10),
                    Margin = new Thickness(0, 0, 0, 8),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(30, fg.R, fg.G, fg.B)),
                    BorderThickness = new Thickness(1),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    { BlurRadius = 6, ShadowDepth = 1, Opacity = 0.06, Color = Colors.Black, Direction = 270 }
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Left badge
                var badge = new Border
                {
                    Width = 56, MinHeight = 76, VerticalAlignment = VerticalAlignment.Stretch,
                    CornerRadius = new CornerRadius(10, 0, 0, 10),
                    Background = new LinearGradientBrush(fg, Color.FromArgb(200, fg.R, fg.G, fg.B), new Point(0, 0), new Point(0, 1))
                };
                badge.Child = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center,
                    Children =
                    {
                        new TextBlock { Text = badgeText, FontSize = 10, Foreground = new SolidColorBrush(Color.FromArgb(180,255,255,255)), HorizontalAlignment = HorizontalAlignment.Center },
                        new TextBlock { Text = num, FontSize = 26, FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0,-2,0,0) }
                    }
                };
                Grid.SetColumn(badge, 0); grid.Children.Add(badge);

                // Center content
                var content = new StackPanel { Margin = new Thickness(16, 10, 12, 10), VerticalAlignment = VerticalAlignment.Center };
                content.Children.Add(new TextBlock { Text = localizedName, FontSize = DS.FontSubtitle, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(fg), Margin = new Thickness(0,0,0,4) });
                var fRow = new WrapPanel { Orientation = Orientation.Horizontal };
                fRow.Children.Add(new TextBlock { Text = formula, FontSize = DS.FontFormula, FontWeight = FontWeights.Bold, FontFamily = DS.FontMath, Foreground = new SolidColorBrush(fg) });
                fRow.Children.Add(new TextBlock { Text = "  =  ", FontSize = DS.FontFormula, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(158,158,158)), VerticalAlignment = VerticalAlignment.Center });
                fRow.Children.Add(new TextBlock { Text = expansion, FontSize = DS.FontFormula, FontWeight = FontWeights.Bold, FontFamily = DS.FontMath, Foreground = new SolidColorBrush(Color.FromRgb(33,33,33)) });
                content.Children.Add(fRow);
                Grid.SetColumn(content, 1); grid.Children.Add(content);

                // Copy button
                var copyBtn = new Button { Content = "📋", FontSize = 18, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Cursor = Cursors.Hand, Padding = new Thickness(12,8,12,8), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,10,0), ToolTip = "Copy", Opacity = 0.4 };
                string ct = $"{formula} = {expansion}";
                copyBtn.Click += (s, e) => { try { Clipboard.SetText(ct); } catch { } ((Button)s).Content = "✅"; var t = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1.2) }; t.Tick += (tt, _) => { ((System.Windows.Threading.DispatcherTimer)tt!).Stop(); ((Button)s).Content = "📋"; }; t.Start(); };
                Grid.SetColumn(copyBtn, 2); grid.Children.Add(copyBtn);

                card.Child = grid;
                card.MouseEnter += (_, _) => { card.Background = new SolidColorBrush(bg); card.BorderBrush = new SolidColorBrush(fg); copyBtn.Opacity = 1; };
                card.MouseLeave += (_, _) => { card.Background = Brushes.White; card.BorderBrush = new SolidColorBrush(Color.FromArgb(30, fg.R, fg.G, fg.B)); copyBtn.Opacity = 0.4; };
                identityPanel.Children.Add(card);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  TAB 2: KIỂM CHỨNG
        // ═══════════════════════════════════════════════════════════
        private void Verify_Changed(object sender, TextChangedEventArgs e) => RunVerification();
        private void Geo_Changed(object sender, TextChangedEventArgs e) => DrawGeometry();
        private void QuickValue_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag) { var p = tag.Split('|'); if (p.Length == 2) { txtA.Text = p[0]; txtB.Text = p[1]; } }
        }

        private void RunVerification()
        {
            if (verifyPanel == null || txtA == null || txtB == null) return;
            verifyPanel.Children.Clear();
            if (!ParsingHelper.TryParseDouble(txtA.Text, out double a) || !ParsingHelper.TryParseDouble(txtB.Text, out double b))
            {
                verifyPanel.Children.Add(new TextBlock { Text = GetStr("Id_ErrorInvalid", "⚠️ Nhập A và B hợp lệ."), FontSize = 15, Foreground = new SolidColorBrush(Color.FromRgb(198,40,40)), Margin = new Thickness(8,8,0,0) });
                return;
            }

            double pw = verifyPanel.ActualWidth > 10 ? verifyPanel.ActualWidth : 950;
            double cw = (pw / 2) - 10;
            if (cw < 350) cw = pw - 10;

            string labelPrefix = GetStr("Id_TabTable", "HĐT").Contains("Identities") ? "Identity" : "HĐT";
            var data = new (string Title, string Left, double LVal, string Right, double RVal, string FgHex)[]
            {
                ($"{labelPrefix} 1: (A+B)²", $"({FmtTerm(a)}+{FmtTerm(b)})²", System.Math.Pow(a+b,2), $"{FmtTerm(a)}²+2·{FmtTerm(a)}·{FmtTerm(b)}+{FmtTerm(b)}²", a*a+2*a*b+b*b, "#1565C0"),
                ($"{labelPrefix} 2: (A−B)²", $"({FmtTerm(a)}−{FmtTerm(b)})²", System.Math.Pow(a-b,2), $"{FmtTerm(a)}²−2·{FmtTerm(a)}·{FmtTerm(b)}+{FmtTerm(b)}²", a*a-2*a*b+b*b, "#7B1FA2"),
                ($"{labelPrefix} 3: A²−B²",  $"{FmtTerm(a)}²−{FmtTerm(b)}²", a*a-b*b, $"({FmtTerm(a)}+{FmtTerm(b)})·({FmtTerm(a)}−{FmtTerm(b)})", (a+b)*(a-b), "#2E7D32"),
                ($"{labelPrefix} 4: (A+B)³", $"({FmtTerm(a)}+{FmtTerm(b)})³", System.Math.Pow(a+b,3), $"{FmtTerm(a)}³+3·{FmtTerm(a)}²·{FmtTerm(b)}+3·{FmtTerm(a)}·{FmtTerm(b)}²+{FmtTerm(b)}³", a*a*a+3*a*a*b+3*a*b*b+b*b*b, "#E65100"),
                ($"{labelPrefix} 5: (A−B)³", $"({FmtTerm(a)}−{FmtTerm(b)})³", System.Math.Pow(a-b,3), $"{FmtTerm(a)}³−3·{FmtTerm(a)}²·{FmtTerm(b)}+3·{FmtTerm(a)}·{FmtTerm(b)}²−{FmtTerm(b)}³", a*a*a-3*a*a*b+3*a*b*b-b*b*b, "#C62828"),
                ($"{labelPrefix} 6: A³+B³",  $"{FmtTerm(a)}³+{FmtTerm(b)}³", a*a*a+b*b*b, $"({FmtTerm(a)}+{FmtTerm(b)})·({FmtTerm(a)}²−{FmtTerm(a)}·{FmtTerm(b)}+{FmtTerm(b)}²)", (a+b)*(a*a-a*b+b*b), "#00695C"),
                ($"{labelPrefix} 7: A³−B³",  $"{FmtTerm(a)}³−{FmtTerm(b)}³", a*a*a-b*b*b, $"({FmtTerm(a)}−{FmtTerm(b)})·({FmtTerm(a)}²+{FmtTerm(a)}·{FmtTerm(b)}+{FmtTerm(b)}²)", (a-b)*(a*a+a*b+b*b), "#283593"),
            };

            foreach (var v in data)
            {
                var fg = (Color)ColorConverter.ConvertFromString(v.FgHex);
                
                // Thuật toán so sánh sai số tương đối (Relative Tolerance) tối ưu cho số lớn
                double diff = System.Math.Abs(v.LVal - v.RVal);
                double maxVal = System.Math.Max(System.Math.Abs(v.LVal), System.Math.Abs(v.RVal));
                bool ok = diff < 1e-9 || (maxVal > 1e-9 && (diff / maxVal) < 1e-12);

                var card = new Border
                {
                    Width = cw, Background = Brushes.White, CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(16, 12, 16, 12), Margin = new Thickness(4),
                    BorderBrush = ok ? new SolidColorBrush(Color.FromRgb(129,199,132)) : new SolidColorBrush(Color.FromRgb(239,154,154)),
                    BorderThickness = new Thickness(0, 0, 0, 3)
                };
                var st = new StackPanel();

                // Title + badge
                var tr = new DockPanel { Margin = new Thickness(0,0,0,6) };
                var bdg = new Border
                {
                    Background = ok ? new SolidColorBrush(Color.FromRgb(200,230,201)) : new SolidColorBrush(Color.FromRgb(255,205,210)),
                    CornerRadius = new CornerRadius(6), Padding = new Thickness(10,3,10,3), HorizontalAlignment = HorizontalAlignment.Right
                };
                bdg.Child = new TextBlock { Text = ok ? "✅ Đúng" : "❌ Sai", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = ok ? new SolidColorBrush(Color.FromRgb(27,94,32)) : new SolidColorBrush(Color.FromRgb(198,40,40)) };
                DockPanel.SetDock(bdg, Dock.Right); tr.Children.Add(bdg);
                tr.Children.Add(new TextBlock { Text = v.Title, FontSize = 15, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(fg), VerticalAlignment = VerticalAlignment.Center });
                st.Children.Add(tr);

                AddVerifyLine(st, "VT", v.Left, v.LVal, fg);
                AddVerifyLine(st, "VP", v.Right, v.RVal, fg);
                card.Child = st;
                verifyPanel.Children.Add(card);
            }
        }

        private static void AddVerifyLine(StackPanel parent, string label, string expr, double val, Color ac)
        {
            var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var badge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(238,238,238)), CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 1, 6, 1), Margin = new Thickness(0, 0, 8, 0),
                Child = new TextBlock { Text = label, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(97,97,97)) }
            };
            Grid.SetColumn(badge, 0);
            grid.Children.Add(badge);

            var formulaBlock = new TextBlock
            {
                Text = $"{expr} = ", FontSize = DS.FontPreset, FontFamily = DS.FontPrimary,
                Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(formulaBlock, 1);
            grid.Children.Add(formulaBlock);

            var valueBlock = new TextBlock
            {
                Text = Fmt(val), FontSize = DS.FontResult, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(ac), Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(valueBlock, 2);
            grid.Children.Add(valueBlock);

            parent.Children.Add(grid);
        }

        private static string Fmt(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return v.ToString();
            return System.Math.Abs(v - System.Math.Round(v)) < 1e-9 ? System.Math.Round(v).ToString("N0") : v.ToString("0.##");
        }

        private static string FmtTerm(double val)
        {
            return val < 0 ? $"({Fmt(val)})" : Fmt(val);
        }

        // ═══════════════════════════════════════════════════════════
        //  TAB 3: MẸO NHỚ — 2 cột
        // ═══════════════════════════════════════════════════════════
        private void BuildTipsPanel()
        {
            if (tipsPanel == null) return;
            tipsPanel.Children.Clear();
            double pw = tipsPanel.ActualWidth > 10 ? tipsPanel.ActualWidth : 950;
            double cw = (pw / 2) - 14;
            if (cw < 350) cw = pw - 14;

            string[] colors = {
                "#1A237E|#E8EAF6",
                "#E65100|#FFF3E0",
                "#2E7D32|#E8F5E9",
                "#C62828|#FCE4EC",
                "#7B1FA2|#F3E5F5",
                "#00695C|#E0F2F1"
            };

            for (int i = 1; i <= 6; i++)
            {
                string title = GetStr($"Id_TipTitle{i}", "");
                string content = GetStr($"Id_TipContent{i}", "");
                if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(content)) continue;

                var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                var parts = colors[i - 1].Split('|');

                AddTip(title, lines, parts[0], parts[1], cw);
            }
        }

        private void AddTip(string title, string[] lines, string fgHex, string bgHex, double width)
        {
            var bg = (Color)ColorConverter.ConvertFromString(bgHex);
            var fg = (Color)ColorConverter.ConvertFromString(fgHex);
            var card = new Border
            {
                Width = width, Background = Brushes.White, CornerRadius = new CornerRadius(10),
                Padding = new Thickness(18, 14, 18, 14), Margin = new Thickness(5),
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, fg.R, fg.G, fg.B)), BorderThickness = new Thickness(1)
            };
            var st = new StackPanel();

            // Title with accent bar
            var tr = new DockPanel { Margin = new Thickness(0,0,0,10) };
            tr.Children.Add(new Border { Background = new SolidColorBrush(fg), Width = 4, CornerRadius = new CornerRadius(2), Margin = new Thickness(0,0,10,0), VerticalAlignment = VerticalAlignment.Stretch });
            tr.Children.Add(new TextBlock { Text = title, FontSize = 16, FontFamily = DS.FontBold, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(fg) });
            st.Children.Add(tr);

            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line)) { st.Children.Add(new Border { Height = 6 }); continue; }
                bool isKey = line.StartsWith("→") || line.StartsWith("💡") || line.StartsWith("🔑");
                var tb = new TextBlock
                {
                    Text = line, FontSize = isKey ? 14 : 13,
                    FontFamily = DS.FontPrimary,
                    FontWeight = isKey ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = isKey ? new SolidColorBrush(fg) : Brushes.Black,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(isKey ? 0 : 8, 2, 0, 2)
                };
                st.Children.Add(tb);
            }
            card.Child = st;
            tipsPanel.Children.Add(card);
        }

        private void BuildGeoSelector()
        {
            if (geoSelectorPanel == null) return;
            geoSelectorPanel.Children.Clear();

            var labels = new[] { "(A+B)\u00B2", "(A\u2212B)\u00B2", "A\u00B2\u2212B\u00B2", "(A+B)\u00B3" };
            var colors = new[] { "#1565C0", "#7B1FA2", "#2E7D32", "#E65100" };

            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                var c = (Color)ColorConverter.ConvertFromString(colors[i]);
                bool active = i == _selectedGeoIdentity;
                var btn = new Border
                {
                    Background = active ? new SolidColorBrush(c) : DS.LightBg(c),
                    CornerRadius = new CornerRadius(DS.RadiusChip),
                    Padding = new Thickness(DS.PadChip, 8, DS.PadChip, 8),
                    Margin = new Thickness(0, 0, DS.TouchGap, DS.TouchGap),
                    Cursor = Cursors.Hand, MinHeight = DS.TouchMinHeight,
                    BorderBrush = new SolidColorBrush(c),
                    BorderThickness = active ? new Thickness(2) : new Thickness(0, 0, 0, 2)
                };
                btn.Child = new TextBlock
                {
                    Text = $"HĐT {i + 1}: {labels[i]}", FontSize = DS.FontPreset,
                    FontWeight = FontWeights.Bold, FontFamily = DS.FontMath,
                    Foreground = active ? Brushes.White : new SolidColorBrush(c),
                    VerticalAlignment = VerticalAlignment.Center
                };
                btn.MouseLeftButtonDown += (_, _) => { _selectedGeoIdentity = idx; BuildGeoSelector(); DrawGeometry(); };
                geoSelectorPanel.Children.Add(btn);
            }
        }

        private void DrawGeometry()
        {
            if (geoCanvas == null || geoTitle == null || geoExplainPanel == null) return;
            if (_isDrawing) return;
            _isDrawing = true;

            try
            {
                geoCanvas.Children.Clear();
                geoExplainPanel.Children.Clear();

                // 1. Phân tích giá trị đầu vào cạnh a và b
                if (!ParsingHelper.TryParseDouble(txtGeoA?.Text, out double a) || a <= 0)
                {
                    ShowGeometryWarning(GetStr("Id_ErrorInvalidA", "⚠️ Giá trị cạnh a không hợp lệ (phải là số dương)."));
                    return;
                }
                if (!ParsingHelper.TryParseDouble(txtGeoB?.Text, out double b) || b <= 0)
                {
                    ShowGeometryWarning(GetStr("Id_ErrorInvalidB", "⚠️ Giá trị cạnh b không hợp lệ (phải là số dương)."));
                    return;
                }

                // 2. Kiểm tra ràng buộc hình học (phép trừ HĐT 2, 3)
                if (_selectedGeoIdentity == 1 || _selectedGeoIdentity == 2)
                {
                    if (b >= a)
                    {
                        ShowGeometryWarning(GetStr("Id_ErrorBGeA", "⚠️ Trong phép trừ hình học, yêu cầu cạnh b nhỏ hơn cạnh a (b < a)."));
                        return;
                    }
                }

                // 3. Kiểm tra giới hạn kích thước hiển thị tối thiểu
                if (a < 0.5 || b < 0.5)
                {
                    ShowGeometryWarning(GetStr("Id_ErrorTooSmall", "⚠️ Kích thước quá nhỏ để hiển thị hình học trực quan (yêu cầu >= 0.5)."));
                    return;
                }

                // 4. Phân luồng vẽ hình
                if (_selectedGeoIdentity == 3)
                {
                    geoCanvas.Visibility = Visibility.Collapsed;
                    panel3D.Visibility = Visibility.Visible;
                    panel3DControls.Visibility = Visibility.Visible;
                    DrawHDT4(a, b);
                }
                else
                {
                    geoCanvas.Visibility = Visibility.Visible;
                    panel3D.Visibility = Visibility.Collapsed;
                    panel3DControls.Visibility = Visibility.Collapsed;

                    switch (_selectedGeoIdentity)
                    {
                        case 0: DrawHDT1(a, b); break;
                        case 1: DrawHDT2(a, b); break;
                        case 2: DrawHDT3(a, b); break;
                    }
                }
            }
            finally
            {
                _isDrawing = false;
            }
        }

        private void ShowGeometryWarning(string message)
        {
            geoTitle.Text = GetStr("Common_Warning", "Cảnh báo");
            geoTitle.Foreground = new SolidColorBrush(DS.Danger);

            geoCanvas.Visibility = Visibility.Collapsed;
            panel3D.Visibility = Visibility.Collapsed;
            panel3DControls.Visibility = Visibility.Collapsed;

            var card = new Border
            {
                Background = DS.LightBg(DS.Danger),
                CornerRadius = new CornerRadius(DS.RadiusChip),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 6),
                BorderBrush = DS.BrushAlpha(DS.Danger, 60),
                BorderThickness = new Thickness(0, 0, 0, 2)
            };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = message,
                FontSize = DS.FontResult,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(DS.Danger),
                TextWrapping = TextWrapping.Wrap
            });
            card.Child = sp;
            geoExplainPanel.Children.Add(card);
        }

        private void SliderExplosion_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => DrawGeometry();

        // ─── HĐT 1: (a+b)² = a² + 2ab + b² ───
        private void DrawHDT1(double a, double b)
        {
            geoTitle.Text = $"(a+b)\u00B2 = a\u00B2 + 2ab + b\u00B2    [a={a}, b={b}]";
            geoTitle.Foreground = DS.Brush(DS.CatMath);

            double total = a + b;
            double S = 350; // canvas size
            double scale = S / total;
            double sa = a * scale, sb = b * scale;
            double ox = 25, oy = 40; // offset

            geoCanvas.Width = S + 100;
            geoCanvas.Height = S + 100;

            // Colors
            var colA2  = Color.FromRgb(33, 150, 243);   // blue — a²
            var colAB  = Color.FromRgb(255, 152, 0);    // orange — ab
            var colB2  = Color.FromRgb(156, 39, 176);   // purple — b²

            // 1. a² — top-left (hatched green like image)
            DrawRect(ox, oy, sa, sa, colA2, $"a\u00B2 = {a * a}");
            DrawHatch(ox, oy, sa, sa, colA2, true);

            // 2. ab — top-right
            DrawRect(ox + sa, oy, sb, sa, colAB, $"ab = {a * b}");

            // 3. ab — bottom-left
            DrawRect(ox, oy + sa, sa, sb, colAB, $"ab = {a * b}");

            // 4. b² — bottom-right (hatched blue like image)
            DrawRect(ox + sa, oy + sa, sb, sb, colB2, $"b\u00B2 = {b * b}");
            DrawHatch(ox + sa, oy + sa, sb, sb, colB2, false);

            // Outer border
            DrawBorder(ox, oy, S, S, 3);

            // Dimension labels
            DrawDimLabel(ox, oy - 18, sa, $"a = {a}", colA2, true);
            DrawDimLabel(ox + sa, oy - 18, sb, $"b = {b}", colB2, true);
            DrawDimLabel(ox - 22, oy, sa, $"a", colA2, false);
            DrawDimLabel(ox - 22, oy + sa, sb, $"b", colB2, false);

            // Right brace label
            DrawBraceLabel(ox + S + 8, oy, S, $"a+b = {total}", Colors.Red);

            // Bottom brace label
            DrawDimLabel(ox, oy + S + 8, S, $"a+b = {total}", Colors.Red, true);

            // Area labels in center of each region
            DrawAreaLabel(ox + sa / 2, oy + sa / 2, "a\u00B2", colA2, 20);
            DrawAreaLabel(ox + sa + sb / 2, oy + sa / 2, "ab", colAB, 18);
            DrawAreaLabel(ox + sa / 2, oy + sa + sb / 2, "ab", colAB, 18);
            DrawAreaLabel(ox + sa + sb / 2, oy + sa + sb / 2, "b\u00B2", colB2, 18);

            // Explanation
            AddGeoStep(GetStr("Id_Geo1_Step1", "Hình vuông lớn có cạnh (a+b)"), $"S = (a+b)\u00B2 = ({a}+{b})\u00B2 = {total * total}", "#1565C0");
            AddGeoStep(GetStr("Id_Geo1_Step2", "Ô trên-trái: hình vuông cạnh a"), $"S\u2081 = a\u00B2 = {a}\u00B2 = {a * a}", "#2196F3");
            AddGeoStep(GetStr("Id_Geo1_Step3", "Ô trên-phải + Ô dưới-trái: 2 hình chữ nhật a×b"), $"S\u2082 + S\u2083 = 2ab = 2\u00D7{a}\u00D7{b} = {2 * a * b}", "#FF9800");
            AddGeoStep(GetStr("Id_Geo1_Step4", "Ô dưới-phải: hình vuông cạnh b"), $"S\u2084 = b\u00B2 = {b}\u00B2 = {b * b}", "#9C27B0");
            AddGeoStep(GetStr("Id_Geo_Proof", "Chứng minh"), $"(a+b)\u00B2 = a\u00B2 + 2ab + b\u00B2 = {a * a} + {2 * a * b} + {b * b} = {total * total} \u2714", "#2E7D32");
        }

        // ─── HĐT 2: (a−b)² = a² − 2ab + b² ───
        private void DrawHDT2(double a, double b)
        {
            geoTitle.Text = $"(a\u2212b)\u00B2 = a\u00B2 \u2212 2ab + b\u00B2    [a={a.ToString("F1")}, b={b.ToString("F1")}]";
            geoTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Identities[1].FgHex));

            double S = 350;
            double scale = S / a;
            double sa = a * scale, sb = b * scale;
            double sc = (a - b) * scale;
            double ox = 25, oy = 40;

            geoCanvas.Width = S + 100;
            geoCanvas.Height = S + 100;

            var colA2  = Color.FromRgb(33, 150, 243);   // Xanh dương
            var colAB  = Color.FromRgb(239, 83, 80);    // Đỏ
            var colAmb = Color.FromRgb(76, 175, 80);    // Xanh lá - (a-b)²
            var colB2  = Color.FromRgb(156, 39, 176);   // Tím - b²

            // Vẽ nền hình vuông a² lớn (xanh dương nhạt)
            DrawRect(ox, oy, sa, sa, Color.FromRgb(200, 220, 240), "");

            // Vẽ hình vuông kết quả (a-b)² màu xanh lá gạch chéo
            DrawRect(ox, oy, sc, sc, colAmb, $"(a-b)² = {(a-b)*(a-b):F2}");
            DrawHatch(ox, oy, sc, sc, colAmb, true);

            // Vẽ hình chữ nhật dọc ab lớn (b × a) nét đứt
            var rectVertical = new System.Windows.Shapes.Rectangle
            {
                Width = sb, Height = sa,
                Fill = new SolidColorBrush(Color.FromArgb(40, colAB.R, colAB.G, colAB.B)),
                Stroke = new SolidColorBrush(colAB),
                StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection { 4, 3 }
            };
            Canvas.SetLeft(rectVertical, ox + sc); Canvas.SetTop(rectVertical, oy);
            geoCanvas.Children.Add(rectVertical);

            // Vẽ hình chữ nhật ngang ab lớn (a × b) nét đứt
            var rectHorizontal = new System.Windows.Shapes.Rectangle
            {
                Width = sa, Height = sb,
                Fill = new SolidColorBrush(Color.FromArgb(40, colAB.R, colAB.G, colAB.B)),
                Stroke = new SolidColorBrush(colAB),
                StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection { 4, 3 }
            };
            Canvas.SetLeft(rectHorizontal, ox); Canvas.SetTop(rectHorizontal, oy + sc);
            geoCanvas.Children.Add(rectHorizontal);

            // Vẽ hình vuông b² màu tím (vùng giao của 2 hình chữ nhật lớn)
            DrawRect(ox + sc, oy + sc, sb, sb, colB2, $"b² = {b*b:F2}");
            DrawHatch(ox + sc, oy + sc, sb, sb, colB2, false); // Gạch chéo ngược

            // Vẽ viền đen hình vuông lớn ngoài cùng
            DrawBorder(ox, oy, sa, sa, 3);
            // Vẽ viền đen hình vuông (a-b)²
            DrawBorder(ox, oy, sc, sc, 2);

            // Nhãn kích thước các cạnh
            DrawDimLabel(ox, oy - 18, sc, $"a\u2212b = {(a - b):F1}", colAmb, true);
            DrawDimLabel(ox + sc, oy - 18, sb, $"b = {b:F1}", colAB, true);
            DrawDimLabel(ox - 22, oy, sc, $"a\u2212b", colAmb, false);
            DrawDimLabel(ox - 22, oy + sc, sb, $"b", colAB, false);
            DrawBraceLabel(ox + sa + 8, oy, sa, $"a = {a:F1}", Colors.Red);
            DrawDimLabel(ox, oy + sa + 8, sa, $"a = {a:F1}", Colors.Red, true);

            // Nhãn diện tích ở tâm mỗi vùng hình học
            DrawAreaLabel(ox + sc / 2, oy + sc / 2, "(a\u2212b)\u00B2", colAmb, 18);
            DrawAreaLabel(ox + sc + sb / 2, oy + sc / 2, "ab", colAB, 18);
            DrawAreaLabel(ox + sc / 2, oy + sc + sb / 2, "ab", colAB, 18);
            DrawAreaLabel(ox + sc + sb / 2, oy + sc + sb / 2, "b\u00B2", colB2, 18);

            // Giải thích logic số học
            AddGeoStep(GetStr("Id_Geo2_Step1", "Hình vuông lớn cạnh a"), $"a\u00B2 = {a}\u00B2 = {a * a:F2}", "#2196F3");
            AddGeoStep(GetStr("Id_Geo2_Step2", "Bớt 2 hình chữ nhật kích thước a×b"), $"\u22122ab = \u22122 \u00D7 {a} \u00D7 {b} = {-2 * a * b:F2}", "#EF5350");
            AddGeoStep(GetStr("Id_Geo2_Step3", "Nhưng hình vuông b² bị bớt 2 lần → cộng lại"), $"+b\u00B2 = +{b * b:F2}", "#9C27B0");
            AddGeoStep(GetStr("Id_Geo2_Step4", "Còn lại diện tích hình vuông (a−b)²"), $"= a\u00B2 \u2212 2ab + b\u00B2 = {a * a:F2} \u2212 {2 * a * b:F2} + {b * b:F2} = {(a - b) * (a - b):F2} \u2714", "#4CAF50");
        }

        // ─── HĐT 3: a² − b² = (a+b)(a−b) ───
        private void DrawHDT3(double a, double b)
        {
            geoTitle.Text = $"a\u00B2 \u2212 b\u00B2 = (a+b)(a\u2212b)    [a={a.ToString("F1")}, b={b.ToString("F1")}]";
            geoTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Identities[2].FgHex));

            double S = 200; // Thu nhỏ kích thước vẽ cơ sở xuống 200 để tránh tràn Canvas
            double scale = S / a;
            double sa = a * scale, sb = b * scale;
            double sc = (a - b) * scale;
            double ox = 25, oy = 40;

            var colA2   = Color.FromRgb(33, 150, 243);   // Xanh dương
            var colB2   = Color.FromRgb(239, 83, 80);    // Đỏ - b² bị cắt
            var colDiff = Color.FromRgb(76, 175, 80);    // Xanh lá nhạt - a²-b²
            var colMove = Color.FromRgb(129, 199, 132);  // Xanh lá sáng - Mảnh di chuyển ghép

            // A. HÌNH BÊN TRÁI: Hình chữ L (a² - b²)
            // Nền hình vuông a²
            DrawRect(ox, oy, sa, sa, colA2, "");
            DrawHatch(ox, oy, sa, sa, colA2, true);

            // Hình vuông b² bị cắt đi ở góc trên bên phải
            DrawRect(ox + sa - sb, oy, sb, sb, colB2, $"b² = {b*b:F2}");
            DrawHatch(ox + sa - sb, oy, sb, sb, colB2, false);

            // Tô màu xanh lá cho phần còn lại (hình chữ L) gồm 2 mảnh:
            DrawRect(ox, oy + sb, sa, sc, colDiff, "a × (a-b)"); // Mảnh ngang dưới
            DrawRect(ox, oy, sa - sb, sb, colDiff, "(a-b) × b"); // Mảnh dọc bên trái (không đè lên góc b²)

            // Vẽ đường viền
            DrawBorder(ox, oy, sa, sa, 3);
            DrawBorder(ox + sa - sb, oy, sb, sb, 2);

            // Vẽ các nhãn kích thước hình bên trái
            DrawDimLabel(ox, oy - 18, sa - sb, $"a\u2212b", colDiff, true);
            DrawDimLabel(ox + sa - sb, oy - 18, sb, $"b = {b:F1}", colB2, true);
            DrawDimLabel(ox - 22, oy, sb, $"b", colB2, false);
            DrawDimLabel(ox - 22, oy + sb, sc, $"a\u2212b", colDiff, false);
            DrawBraceLabel(ox + sa + 8, oy, sa, $"a = {a:F1}", Colors.Red);
            DrawDimLabel(ox, oy + sa + 8, sa, $"a = {a:F1}", Colors.Red, true);

            // B. MŨI TÊN CHUYỂN ĐỔI (Ở GIỮA)
            double arrowX = ox + sa + 35;
            var arrowTb = new TextBlock
            {
                Text = "\u27A1", FontSize = 30, Foreground = DS.Brush(DS.BrandAccent),
                FontFamily = DS.FontMath
            };
            Canvas.SetLeft(arrowTb, arrowX - 10); Canvas.SetTop(arrowTb, oy + sb + sc / 2 - 15);
            geoCanvas.Children.Add(arrowTb);

            // C. HÌNH BÊN PHẢI: Hình chữ nhật ghép mới (a+b) × (a-b)
            double rx = arrowX + 50; // Toạ độ bắt đầu hình ghép bên phải
            
            // Vẽ mảnh chính gốc a × (a-b) bên trái hình ghép
            DrawRect(rx, oy + sb, sa, sc, colDiff, $"a × (a-b) = {a * (a-b):F2}");
            
            // Vẽ mảnh ghép thêm b × (a-b) di chuyển từ trên xuống đặt vào bên phải mảnh chính
            DrawRect(rx + sa, oy + sb, sb, sc, colMove, $"b × (a-b) = {b * (a-b):F2}");
            DrawHatch(rx + sa, oy + sb, sb, sc, colMove, true); // Gạch chéo mảnh ghép thêm

            // Vẽ viền đen đậm bao quanh hình chữ nhật ghép lớn
            DrawBorder(rx, oy + sb, sa + sb, sc, 3);

            // Vẽ các nhãn kích thước hình ghép bên phải
            DrawDimLabel(rx, oy + sb - 18, sa + sb, $"a+b = {a + b:F1}", Colors.Red, true);
            DrawBraceLabel(rx + sa + sb + 8, oy + sb, sc, $"a\u2212b = {(a - b):F1}", colDiff);

            // Thiết lập chiều rộng Canvas động để chứa vừa khít cả hai hình vẽ
            geoCanvas.Width = rx + sa + sb + 80;
            geoCanvas.Height = sa + 80;

            // Các bước giải thích toán học chi tiết bên dưới
            AddGeoStep(GetStr("Id_Geo3_Step1", "Hình vuông lớn cạnh a"), $"a\u00B2 = {a}\u00B2 = {a * a:F2}", "#2196F3");
            AddGeoStep(GetStr("Id_Geo3_Step2", "Cắt bỏ hình vuông nhỏ cạnh b ở góc trên-phải"), $"\u2212b\u00B2 = \u2212{b}\u00B2 = \u2212{b * b:F2}", "#EF5350");
            AddGeoStep(GetStr("Id_Geo3_Step3", "Diện tích phần còn lại (hình chữ L)"), $"S = a\u00B2 \u2212 b\u00B2 = {a * a:F2} \u2212 {b * b:F2} = {a * a - b * b:F2}", "#4CAF50");
            AddGeoStep(GetStr("Id_Geo3_Step4", "Xoay và xếp lại thành hình chữ nhật kích thước (a+b) × (a-b)"), $"S = (a+b) \u00D7 (a\u2212b) = ({a} + {b}) \u00D7 ({a} \u2212 {b}) = {a + b} \u00D7 {a - b} = {(a + b) * (a - b):F2} \u2714", "#2E7D32");
        }

        // ─── Canvas Drawing Helpers ───

        private void DrawRect(double x, double y, double w, double h, Color fill, string tooltip)
        {
            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = w, Height = h,
                Fill = new SolidColorBrush(Color.FromArgb(60, fill.R, fill.G, fill.B)),
                Stroke = new SolidColorBrush(Color.FromArgb(180, fill.R, fill.G, fill.B)),
                StrokeThickness = 1.5
            };
            if (!string.IsNullOrEmpty(tooltip)) rect.ToolTip = tooltip;
            Canvas.SetLeft(rect, x); Canvas.SetTop(rect, y);
            geoCanvas.Children.Add(rect);
        }

        private void DrawHatch(double x, double y, double w, double h, Color c, bool forward)
        {
            double step = 12;
            var brush = new SolidColorBrush(Color.FromArgb(100, c.R, c.G, c.B));
            for (double d = step; d < w + h; d += step)
            {
                double x1, y1, x2, y2;
                if (forward)
                {
                    x1 = x + System.Math.Min(d, w); y1 = y + System.Math.Max(0, d - w);
                    x2 = x + System.Math.Max(0, d - h); y2 = y + System.Math.Min(d, h);
                }
                else
                {
                    x1 = x + w - System.Math.Min(d, w); y1 = y + System.Math.Max(0, d - w);
                    x2 = x + w - System.Math.Max(0, d - h); y2 = y + System.Math.Min(d, h);
                }
                var line = new System.Windows.Shapes.Line
                {
                    X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                    Stroke = brush, StrokeThickness = 1
                };
                geoCanvas.Children.Add(line);
            }
        }

        private void DrawBorder(double x, double y, double w, double h, double thickness)
        {
            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = w, Height = h,
                Fill = Brushes.Transparent, Stroke = Brushes.Black,
                StrokeThickness = thickness
            };
            Canvas.SetLeft(rect, x); Canvas.SetTop(rect, y);
            geoCanvas.Children.Add(rect);
        }

        private void DrawDimLabel(double x, double y, double span, string text, Color c, bool horizontal)
        {
            var tb = new TextBlock
            {
                Text = text, FontSize = DS.FontPreset, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontMath, Foreground = new SolidColorBrush(c)
            };
            if (horizontal)
            {
                Canvas.SetLeft(tb, x + span / 2 - text.Length * 3.5);
                Canvas.SetTop(tb, y);
            }
            else
            {
                tb.LayoutTransform = new RotateTransform(-90);
                Canvas.SetLeft(tb, x);
                Canvas.SetTop(tb, y + span / 2 + text.Length * 3.5);
            }
            geoCanvas.Children.Add(tb);
        }

        private void DrawAreaLabel(double cx, double cy, string text, Color c, double fontSize)
        {
            var tb = new TextBlock
            {
                Text = text, FontSize = fontSize, FontWeight = FontWeights.ExtraBold,
                FontFamily = DS.FontMath, Foreground = new SolidColorBrush(c),
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetLeft(tb, cx - text.Length * fontSize * 0.3);
            Canvas.SetTop(tb, cy - fontSize * 0.7);
            geoCanvas.Children.Add(tb);
        }

        private void DrawBraceLabel(double x, double y, double h, string text, Color c)
        {
            var brush = new SolidColorBrush(c);
            double bx = x + 6;       // brace x start
            double bw = 14;          // brace width (horizontal extent)
            double mid = y + h / 2;  // midpoint

            // Draw right curly brace using 3 segments: top arc, middle tip, bottom arc
            // Top segment: (bx, y) curve right to (bx+bw, mid-4)
            var topLine = new System.Windows.Shapes.Path
            {
                Stroke = brush, StrokeThickness = 2.5, Fill = Brushes.Transparent,
                Data = Geometry.Parse($"M {bx},{y} C {bx},{y + h * 0.1} {bx + bw},{mid - h * 0.15} {bx + bw},{mid}")
            };
            geoCanvas.Children.Add(topLine);

            // Bottom segment: (bx+bw, mid) curve back to (bx, y+h)
            var bottomLine = new System.Windows.Shapes.Path
            {
                Stroke = brush, StrokeThickness = 2.5, Fill = Brushes.Transparent,
                Data = Geometry.Parse($"M {bx + bw},{mid} C {bx + bw},{mid + h * 0.15} {bx},{y + h * 0.9} {bx},{y + h}")
            };
            geoCanvas.Children.Add(bottomLine);

            // Tip triangle at midpoint
            var tip = new System.Windows.Shapes.Polygon
            {
                Fill = brush,
                Points = new PointCollection
                {
                    new Point(bx + bw, mid - 4),
                    new Point(bx + bw + 5, mid),
                    new Point(bx + bw, mid + 4)
                }
            };
            geoCanvas.Children.Add(tip);

            // Text label — positioned to the right of brace
            var label = new TextBlock
            {
                Text = text, FontSize = DS.FontResult, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontMath, Foreground = brush
            };
            Canvas.SetLeft(label, bx + bw + 10);
            Canvas.SetTop(label, mid - 8);
            geoCanvas.Children.Add(label);
        }

        private void AddGeoStep(string title, string detail, string colorHex)
        {
            var c = (Color)ColorConverter.ConvertFromString(colorHex);
            var card = new Border
            {
                Background = DS.LightBg(c),
                CornerRadius = new CornerRadius(DS.RadiusChip),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 6),
                BorderBrush = DS.BrushAlpha(c, 60),
                BorderThickness = new Thickness(0, 0, 0, 2)
            };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = title, FontSize = DS.FontResult, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontMath, Foreground = new SolidColorBrush(c)
            });
            sp.Children.Add(new TextBlock
            {
                Text = detail, FontSize = DS.FontResult, FontFamily = DS.FontMath,
                Foreground = new SolidColorBrush(Color.FromRgb(55, 55, 55)),
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0)
            });
            card.Child = sp;
            geoExplainPanel.Children.Add(card);
        }

        private void OpenGraph_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                double a = 3, b = 2;
                ParsingHelper.TryParseDouble(txtA?.Text, out a);
                ParsingHelper.TryParseDouble(txtB?.Text, out b);
                var ci = System.Globalization.CultureInfo.InvariantCulture;

                // Tính toán biên đồ thị động dựa trên a và b
                double absA = System.Math.Abs(a);
                double absB = System.Math.Abs(b);
                double maxVal = System.Math.Max(absA, absB);
                if (maxVal < 1) maxVal = 5;

                double xMin = -absB - maxVal * 0.5 - 2;
                double xMax = maxVal + 5;
                double yMin = -5;
                double yMax = System.Math.Pow(maxVal + 3, 2);

                string expJs = $@"
        // HĐT 1: (x+b)² = x²+2bx+b²
        calc.setExpression({{id:'h1', latex:'y=(x+{b.ToString(ci)})^2', color:'#1565C0', lineWidth:3, label:'(x+{b.ToString(ci)})²', showLabel:true}});
        calc.setExpression({{id:'h1_rhs', latex:'y=x^2+2\\cdot {b.ToString(ci)}\\cdot x+{b.ToString(ci)}^2', color:'#E53935', lineWidth:2, lineStyle:'DASHED', label:'x²+2·{b.ToString(ci)}x+{b.ToString(ci)}²', showLabel:false}});
        
        // HĐT 2: (x-b)² = x²-2bx+b²
        calc.setExpression({{id:'h2', latex:'y=(x-{b.ToString(ci)})^2', color:'#7B1FA2', lineWidth:2, label:'(x-{b.ToString(ci)})²', showLabel:true}});
        calc.setExpression({{id:'h2_rhs', latex:'y=x^2-2\\cdot {b.ToString(ci)}\\cdot x+{b.ToString(ci)}^2', color:'#D32F2F', lineWidth:2, lineStyle:'DASHED', label:'x²-2·{b.ToString(ci)}x+{b.ToString(ci)}²', showLabel:false}});
        
        // HĐT 3: x²-b² = (x+b)(x-b)
        calc.setExpression({{id:'h3', latex:'y=x^2-{b.ToString(ci)}^2', color:'#2E7D32', lineWidth:2, label:'x²-{b.ToString(ci)}²', showLabel:true}});
        calc.setExpression({{id:'h3_rhs', latex:'y=(x+{b.ToString(ci)})(x-{b.ToString(ci)})', color:'#388E3C', lineWidth:2, lineStyle:'DASHED', label:'(x+{b.ToString(ci)})(x-{b.ToString(ci)})', showLabel:false}});
        
        // Đường gióng thẳng đứng tại x = a
        calc.setExpression({{id:'x_val', latex:'x={a.ToString(ci)}', color:'#E65100', lineStyle:'DASHED', lineWidth:1.5, label:'x = a ({a.ToString(ci)})', showLabel:true}});
        
        calc.setMathBounds({{ left: {xMin.ToString(ci)}, right: {xMax.ToString(ci)}, bottom: {yMin.ToString(ci)}, top: {yMax.ToString(ci)} }});";

                var win = new GraphWindow(expJs, $"📐 HĐT — A={a}, B={b}");
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (System.Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
        }

        // ═══════════════════════════════════════════════════════════
        //  ĐỒ HOẠ 3D CHO HẰNG ĐẲNG THỨC BẬC 3
        // ═══════════════════════════════════════════════════════════

        private void Viewport3D_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _lastMousePosition = e.GetPosition(viewport3D);
            _isMouseCaptured = true;
            viewport3D.CaptureMouse();
        }

        private void Viewport3D_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isMouseCaptured) return;
            Point currentPosition = e.GetPosition(viewport3D);
            double dx = currentPosition.X - _lastMousePosition.X;
            double dy = currentPosition.Y - _lastMousePosition.Y;

            _angleY += dx * 0.5; // Xoay quanh trục Y
            _angleX += dy * 0.5; // Xoay quanh trục X

            // Giới hạn góc xoay dọc để tránh lộn ngược camera
            if (_angleX > 85) _angleX = 85;
            if (_angleX < -85) _angleX = -85;

            _lastMousePosition = currentPosition;
            UpdateCameraTransform();
        }

        private void Viewport3D_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isMouseCaptured = false;
            viewport3D.ReleaseMouseCapture();
        }

        private void UpdateCameraTransform()
        {
            if (viewport3D == null) return;

            double radius = 700; // Khoảng cách từ camera đến tâm O(0,0,0)
            double radX = _angleX * System.Math.PI / 180;
            double radY = _angleY * System.Math.PI / 180;

            double x = radius * System.Math.Cos(radX) * System.Math.Sin(radY);
            double y = radius * System.Math.Sin(radX);
            double z = radius * System.Math.Cos(radX) * System.Math.Cos(radY);

            var camera = new System.Windows.Media.Media3D.PerspectiveCamera
            {
                Position = new System.Windows.Media.Media3D.Point3D(x, y, z),
                LookDirection = new System.Windows.Media.Media3D.Vector3D(-x, -y, -z),
                UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0),
                FieldOfView = 45
            };
            viewport3D.Camera = camera;
        }

        private System.Windows.Media.Media3D.GeometryModel3D CreateCubeModel(double cx, double cy, double cz, double w, double h, double d, Color color)
        {
            var mesh = new System.Windows.Media.Media3D.MeshGeometry3D();
            double dx = w / 2;
            double dy = h / 2;
            double dz = d / 2;

            // 8 đỉnh khối hộp
            var points = new System.Windows.Media.Media3D.Point3D[]
            {
                new(cx - dx, cy - dy, cz - dz), // 0
                new(cx + dx, cy - dy, cz - dz), // 1
                new(cx + dx, cy + dy, cz - dz), // 2
                new(cx - dx, cy + dy, cz - dz), // 3
                new(cx - dx, cy - dy, cz + dz), // 4
                new(cx + dx, cy - dy, cz + dz), // 5
                new(cx + dx, cy + dy, cz + dz), // 6
                new(cx - dx, cy + dy, cz + dz)  // 7
            };

            foreach (var p in points) mesh.Positions.Add(p);

            // 12 mặt tam giác (đóng kín khối hộp)
            int[] indices = new int[]
            {
                0, 2, 1,   0, 3, 2, // Back
                4, 5, 6,   4, 6, 7, // Front
                0, 7, 3,   0, 4, 7, // Left
                1, 6, 5,   1, 2, 6, // Right
                3, 6, 2,   3, 7, 6, // Top
                0, 1, 5,   0, 5, 4  // Bottom
            };

            foreach (var idx in indices) mesh.TriangleIndices.Add(idx);

            var brush = new SolidColorBrush(Color.FromArgb(180, color.R, color.G, color.B));
            var material = new System.Windows.Media.Media3D.MaterialGroup();
            material.Children.Add(new System.Windows.Media.Media3D.DiffuseMaterial(brush));
            material.Children.Add(new System.Windows.Media.Media3D.SpecularMaterial(Brushes.White, 30));

            var model = new System.Windows.Media.Media3D.GeometryModel3D(mesh, material);
            model.BackMaterial = new System.Windows.Media.Media3D.DiffuseMaterial(brush);
            return model;
        }

        private void DrawHDT4(double a, double b)
        {
            geoTitle.Text = $"(a+b)\u00B3 = a\u00B3 + 3a\u00B2b + 3ab\u00B2 + b\u00B3    [a={a:F1}, b={b:F1}]";
            geoTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Identities[3].FgHex));

            viewport3D.Children.Clear();

            // 1. Nguồn sáng
            var lightGroup = new System.Windows.Media.Media3D.Model3DGroup();
            lightGroup.Children.Add(new System.Windows.Media.Media3D.AmbientLight(Color.FromRgb(100, 100, 100)));
            lightGroup.Children.Add(new System.Windows.Media.Media3D.DirectionalLight(Color.FromRgb(180, 180, 180), new System.Windows.Media.Media3D.Vector3D(-1, -2, -3)));
            lightGroup.Children.Add(new System.Windows.Media.Media3D.DirectionalLight(Color.FromRgb(80, 80, 80), new System.Windows.Media.Media3D.Vector3D(1, 2, 3)));
            viewport3D.Children.Add(new System.Windows.Media.Media3D.ModelVisual3D { Content = lightGroup });

            // 2. Định nghĩa toạ độ 3D và kích thước
            double desiredSize = 250;
            double scale = desiredSize / (a + b);
            double sa = a * scale;
            double sb = b * scale;
            double total = sa + sb;

            double offset = (total / 2) - sb;

            double valL = -total / 2 + sa / 2; // Tâm phần đoạn lớn a
            double valS = offset + sb / 2;     // Tâm phần đoạn nhỏ b

            var colA3 = Color.FromRgb(33, 150, 243);  // Xanh dương - a³
            var colA2b = Color.FromRgb(255, 152, 0);  // Cam - a²b
            var colAb2 = Color.FromRgb(156, 39, 176); // Tím - ab²
            var colB3 = Color.FromRgb(76, 175, 80);   // Xanh lá - b³

            // Khởi tạo 8 khối cấu thành khối lập phương (a+b)³
            var blocks = new (string Name, double W, double H, double D, double CX, double CY, double CZ, Color Col)[]
            {
                ("a\u00B3", sa, sa, sa, valL, valL, valL, colA3),

                ("a\u00B2b", sa, sa, sb, valL, valL, valS, colA2b),
                ("a\u00B2b", sa, sb, sa, valL, valS, valL, colA2b),
                ("a\u00B2b", sb, sa, sa, valS, valL, valL, colA2b),

                ("ab\u00B2", sa, sb, sb, valL, valS, valS, colAb2),
                ("ab\u00B2", sb, sa, sb, valS, valL, valS, colAb2),
                ("ab\u00B2", sb, sb, sa, valS, valS, valL, colAb2),

                ("b\u00B3", sb, sb, sb, valS, valS, valS, colB3)
            };

            _modelGroup = new System.Windows.Media.Media3D.Model3DGroup();
            double eFactor = sliderExplosion != null ? sliderExplosion.Value : 0;
            double shift = 1.0 + eFactor * 0.7; // Tỉ lệ giãn cách phân rã

            foreach (var bld in blocks)
            {
                double cx = bld.CX * shift;
                double cy = bld.CY * shift;
                double cz = bld.CZ * shift;

                var cube = CreateCubeModel(cx, cy, cz, bld.W, bld.H, bld.D, bld.Col);
                _modelGroup.Children.Add(cube);
            }

            viewport3D.Children.Add(new System.Windows.Media.Media3D.ModelVisual3D { Content = _modelGroup });

            UpdateCameraTransform();

            // Chú giải số học
            AddGeoStep(GetStr("Id_Geo4_Step1", "Khối lập phương lớn cạnh (a+b)"), $"V = (a+b)\u00B3 = ({a}+{b})\u00B3 = {System.Math.Pow(a+b, 3):F2}", "#1565C0");
            AddGeoStep(GetStr("Id_Geo4_Step2", "1 khối lập phương lớn cạnh a"), $"V\u2081 = a\u00B3 = {a}\u00B3 = {a*a*a:F2}", "#2196F3");
            AddGeoStep(GetStr("Id_Geo4_Step3", "3 khối hộp chữ nhật kích thước a×a×b"), $"3\u00D7V\u2082 = 3a\u00B2b = 3\u00D7{a}\u00B2\u00D7{b} = {3*a*a*b:F2}", "#FF9800");
            AddGeoStep(GetStr("Id_Geo4_Step4", "3 khối hộp chữ nhật kích thước a×b×b"), $"3\u00D7V\u2083 = 3ab\u00B2 = 3\u00D7{a}\u00D7{b}\u00B2 = {3*a*b*b:F2}", "#9C27B0");
            AddGeoStep(GetStr("Id_Geo4_Step5", "1 khối lập phương nhỏ cạnh b"), $"V\u2084 = b\u00B3 = {b}\u00B3 = {b*b*b:F2}", "#4CAF50");
            AddGeoStep(GetStr("Id_Geo_Proof3D", "Chứng minh bằng thể tích"), $"(a+b)\u00B3 = a\u00B3 + 3a\u00B2b + 3ab\u00B2 + b\u00B3 = {a*a*a:F2} + {3*a*a*b:F2} + {3*a*b*b:F2} + {b*b*b:F2} = {System.Math.Pow(a+b,3):F2} \u2714", "#2E7D32");
        }

        private void ResetView_Click(object sender, RoutedEventArgs e)
        {
            _angleX = 25;
            _angleY = -35;
            UpdateCameraTransform();
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
                        Icon = "🧠",
                        Title = isVN ? "Tính nhẩm bình phương số lớn" : "Fast Mental Squaring",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_identities_1_{suffix}.png",
                        Description = isVN 
                            ? "Sử dụng hằng đẳng thức (a-b)² hoặc (a+b)² để tính nhẩm nhanh bình phương các số lớn gần số tròn chục. Ví dụ: 99² = (100-1)² = 10000 - 200 + 1 = 9801." 
                            : "Use the identity (a-b)² or (a+b)² to square large numbers near multiples of 10 in your head. E.g., 99² = (100-1)² = 10000 - 200 + 1 = 9801."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🗺️",
                        Title = isVN ? "Phân chia thửa đất hình vuông" : "Land Parcel Division",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_identities_2_{suffix}.png",
                        Description = isVN 
                            ? "Khi quy hoạch mở rộng mảnh đất hình vuông cạnh a thêm b đơn vị độ dài, diện tích mới (a+b)² được chia nhanh thành a² (khu đất cũ), 2ab (hai dải đất biên), và b² (góc đất phụ) để tính toán đền bù giải phóng mặt bằng." 
                            : "When expanding a square land plot of side a by width b, the new area (a+b)² is subdivided into a² (old area), 2ab (two side strips), and b² (corner sub-plot) to simplify compensation math."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💻",
                        Title = isVN ? "Tối ưu hóa thuật toán nhân số lớn" : "Large Number Multiplication",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_identities_3_{suffix}.png",
                        Description = isVN 
                            ? "Thuật toán nhân Karatsuba trong mật mã học máy tính sử dụng các hằng đẳng thức đại số để chuyển đổi phép nhân ab thành phép tính tổng hiệu và bình phương, giảm từ 4 phép nhân xuống còn 3 phép nhân để xử lý dữ liệu lớn nhanh hơn." 
                            : "The Karatsuba multiplication algorithm in cryptography uses algebraic identities to convert multiplication ab into additions and squares, reducing complexity from 4 basic multiplications to 3 for faster big-data processing."
                    }
                };

                if (practicalAppViewer != null) practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for IdentityTool: {Err}", ex.Message);
            }
        }

        private void ApplyGraphicsSettings()
        {
            int resolutionSetting = QASmartTouch.Services.AppSettings.GetRecommended3DResolution();
            var config = QASmartClass.Services.AppConfig.Load();
            if (resolutionSetting <= 20 || config.Disable3DAntiAliasing || (config.AutoOptimizeForIntegratedGraphics && QASmartClass.Services.AppConfig.IsIntegratedGraphicsDetected))
            {
                System.Windows.Media.RenderOptions.SetEdgeMode(viewport3D, System.Windows.Media.EdgeMode.Aliased);
            }
            else
            {
                System.Windows.Media.RenderOptions.SetEdgeMode(viewport3D, System.Windows.Media.EdgeMode.Unspecified);
            }
        }
    }
}

