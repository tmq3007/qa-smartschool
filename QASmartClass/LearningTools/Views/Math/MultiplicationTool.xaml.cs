using QASmartClass.LearningTools.Models;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using QASmartClass.LearningTools.Controls;

namespace QASmartClass.LearningTools.Views.Math
{
    public partial class MultiplicationTool : BaseToolControl
    {
        private readonly Random _rng = new();
        private int _selectedTable = 2;  // Bảng nhân đang xem chi tiết
        private bool _isLoadedInitialized;

        // Flash card state
        private int _flashA, _flashB;
        private bool _flashRevealed;
        private int _flashTableFilter = 0; // 0 = tất cả

        // Quiz state
        private int _quizA, _quizB;
        private int _quizCorrect, _quizTotal, _quizTarget = 10;
        private readonly Stopwatch _quizStopwatch = new();
        private readonly DispatcherTimer _quizTimer;
        private DispatcherTimer? _transitionTimer;
        private bool _quizActive;
        private bool _isWaitingForNextQuestion;
        private int _quizTableFilter = 0; // 0 = tất cả

        // Image export
        private RenderTargetBitmap? _lastRenderedImage;

        // Colors for each table (Vietnamese style — colorful cards)
        private static readonly Color[] TableColors =
        {
            Color.FromRgb(239, 83, 80),    // 1 - Đỏ
            Color.FromRgb(255, 152, 0),    // 2 - Cam
            Color.FromRgb(255, 193, 7),    // 3 - Vàng
            Color.FromRgb(76, 175, 80),    // 4 - Xanh lá
            Color.FromRgb(0, 150, 136),    // 5 - Teal
            Color.FromRgb(33, 150, 243),   // 6 - Xanh dương
            Color.FromRgb(63, 81, 181),    // 7 - Indigo
            Color.FromRgb(156, 39, 176),   // 8 - Tím
            Color.FromRgb(233, 30, 99),    // 9 - Hồng
            Color.FromRgb(121, 85, 72),    // 10 - Nâu
        };

        private static readonly Color[] TableBgColors =
        {
            Color.FromRgb(255, 235, 238),  // 1
            Color.FromRgb(255, 243, 224),  // 2
            Color.FromRgb(255, 249, 196),  // 3
            Color.FromRgb(232, 245, 233),  // 4
            Color.FromRgb(224, 242, 241),  // 5
            Color.FromRgb(227, 242, 253),  // 6
            Color.FromRgb(232, 234, 246),  // 7
            Color.FromRgb(243, 229, 245),  // 8
            Color.FromRgb(252, 228, 236),  // 9
            Color.FromRgb(239, 235, 233),  // 10
        };

        public MultiplicationTool()
        {
            InitializeComponent();
            // Attach TouchNumPad
            QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtQuizAnswer, step: 1, allowDecimal: false);

            _quizTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _quizTimer.Tick += (_, _) =>
            {
                if (_quizActive)
                {
                    var ts = _quizStopwatch.Elapsed;
                    txtQuizTimer.Text = $"⏱️ {ts.Minutes:D2}:{ts.Seconds:D2}";
                }
            };

            Loaded += (_, _) =>
            {
                if (_isLoadedInitialized) return;
                _isLoadedInitialized = true;

                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Ví dụ" : "Guide & Examples";
                if (menuTextAll != null) menuTextAll.Text = isVN ? "Tất cả bảng" : "All Tables";
                if (menuTextSingle != null) menuTextSingle.Text = isVN ? "Xem từng bảng" : "View Single";
                if (menuTextFlash != null) menuTextFlash.Text = isVN ? "Flash Cards" : "Flash Cards";
                if (menuTextQuiz != null) menuTextQuiz.Text = isVN ? "Quiz tốc độ" : "Speed Quiz";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                BuildAllTablesView();
                BuildSelectorButtons();
                ShowSingleTable(2);
                InitFlashComboBox();
                InitQuizComboBox();
                GenerateFlashCard();
                LoadPracticalApps();
            };

            Unloaded += (_, _) =>
            {
                _quizTimer?.Stop();
                _transitionTimer?.Stop();
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  TAB 1: TẤT CẢ CÁC BẢNG (Card Grid)
        // ═══════════════════════════════════════════════════════════

        private void BuildAllTablesView()
        {
            if (wrapAllTables == null) return;
            wrapAllTables.Children.Clear();

            for (int n = 1; n <= 10; n++)
            {
                var card = CreateTableCard(n, 280, 380);
                card.Margin = new Thickness(6);
                card.Cursor = Cursors.Hand;

                int captured = n;
                card.MouseLeftButtonDown += (s, e) =>
                {
                    // Switch to detail tab
                    _selectedTable = captured;
                    if (sideMenu != null) sideMenu.SelectedIndex = 0;
                    UpdateSelectorHighlight();
                    ShowSingleTable(captured);
                };

                wrapAllTables.Children.Add(card);
            }
        }

        /// <summary>Tạo 1 card bảng cửu chương N (kiểu Việt Nam)</summary>
        private Border CreateTableCard(int n, double width, double height)
        {
            int idx = (n - 1) % TableColors.Length;
            var accentColor = TableColors[idx];
            var bgColor = TableBgColors[idx];

            var card = new Border
            {
                Width = width,
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(bgColor),
                BorderBrush = new SolidColorBrush(accentColor),
                BorderThickness = new Thickness(2.5),
                Padding = new Thickness(0),
                SnapsToDevicePixels = true,
            };

            card.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 8,
                ShadowDepth = 2,
                Opacity = 0.1,
                Color = Colors.Black
            };

            var sp = new StackPanel();

            // Header
            var header = new Border
            {
                Background = new SolidColorBrush(accentColor),
                CornerRadius = new CornerRadius(9, 9, 0, 0),
                Padding = new Thickness(12, 8, 12, 8),
            };
            var headerText = new TextBlock
            {
                Text = $"Bảng nhân {n}",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontFamily = new FontFamily("Segoe UI"),
            };
            header.Child = headerText;
            sp.Children.Add(header);

            // Content: Grid 19 rows (10 formulas + 9 dividers), 7 columns for centering and alignment
            var contentGrid = new Grid { Margin = new Thickness(16, 10, 16, 12) };
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Col 0: Left space
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Col 1: Factor 1
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Col 2: ×
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Col 3: Factor 2
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Col 4: =
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Col 5: Result
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Col 6: Right space

            for (int i = 1; i <= 10; i++)
            {
                int rIdx = (i - 1) * 2;
                contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                if (i < 10)
                {
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                }

                var txtF1 = new TextBlock
                {
                    Text = n.ToString(),
                    FontSize = 15,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(55, 71, 79)),
                    FontFamily = new FontFamily("Segoe UI"),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetRow(txtF1, rIdx);
                Grid.SetColumn(txtF1, 1);
                contentGrid.Children.Add(txtF1);

                var txtSign = new TextBlock
                {
                    Text = "×",
                    FontSize = 15,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(55, 71, 79)),
                    FontFamily = new FontFamily("Segoe UI"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(6, 0, 6, 0),
                };
                Grid.SetRow(txtSign, rIdx);
                Grid.SetColumn(txtSign, 2);
                contentGrid.Children.Add(txtSign);

                var txtF2 = new TextBlock
                {
                    Text = i.ToString(),
                    FontSize = 15,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(55, 71, 79)),
                    FontFamily = new FontFamily("Segoe UI"),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetRow(txtF2, rIdx);
                Grid.SetColumn(txtF2, 3);
                contentGrid.Children.Add(txtF2);

                var txtEq = new TextBlock
                {
                    Text = "=",
                    FontSize = 15,
                    Foreground = new SolidColorBrush(accentColor),
                    FontWeight = FontWeights.Bold,
                    FontFamily = new FontFamily("Segoe UI"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 8, 0),
                };
                Grid.SetRow(txtEq, rIdx);
                Grid.SetColumn(txtEq, 4);
                contentGrid.Children.Add(txtEq);

                var txtRes = new TextBlock
                {
                    Text = $"{n * i}",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(accentColor),
                    FontFamily = new FontFamily("Segoe UI"),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetRow(txtRes, rIdx);
                Grid.SetColumn(txtRes, 5);
                contentGrid.Children.Add(txtRes);

                // Divider line (except last)
                if (i < 10)
                {
                    var line = new Rectangle
                    {
                        Height = 1,
                        Fill = new SolidColorBrush(Color.FromArgb(40, accentColor.R, accentColor.G, accentColor.B)),
                        Margin = new Thickness(0, 3, 0, 3),
                    };
                    Grid.SetRow(line, rIdx + 1);
                    Grid.SetColumn(line, 1);
                    Grid.SetColumnSpan(line, 5);
                    contentGrid.Children.Add(line);
                }
            }
            sp.Children.Add(contentGrid);

            card.Child = sp;
            return card;
        }

        // ═══════════════════════════════════════════════════════════
        //  TAB 2: XEM TỪNG BẢNG CHI TIẾT
        // ═══════════════════════════════════════════════════════════

        private void BuildSelectorButtons()
        {
            if (wrapSelector == null) return;
            wrapSelector.Children.Clear();

            for (int n = 1; n <= 10; n++)
            {
                int idx = (n - 1) % TableColors.Length;
                var color = TableColors[idx];
                int captured = n;

                var btn = new Border
                {
                    Width = 52, Height = 36,
                    CornerRadius = new CornerRadius(8),
                    Background = n == _selectedTable
                        ? new SolidColorBrush(color)
                        : new SolidColorBrush(TableBgColors[idx]),
                    Margin = new Thickness(3),
                    Cursor = Cursors.Hand,
                    Tag = n,
                };
                var tb = new TextBlock
                {
                    Text = $"×{n}",
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = n == _selectedTable
                        ? Brushes.White
                        : new SolidColorBrush(color),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontFamily = new FontFamily("Segoe UI"),
                };
                btn.Child = tb;
                btn.MouseLeftButtonDown += (s, e) =>
                {
                    _selectedTable = captured;
                    UpdateSelectorHighlight();
                    ShowSingleTable(captured);
                };
                wrapSelector.Children.Add(btn);
            }
        }

        private void UpdateSelectorHighlight()
        {
            if (wrapSelector == null) return;
            foreach (var child in wrapSelector.Children)
            {
                if (child is Border bd && bd.Tag is int n)
                {
                    int idx = (n - 1) % TableColors.Length;
                    bool selected = n == _selectedTable;
                    bd.Background = selected
                        ? new SolidColorBrush(TableColors[idx])
                        : new SolidColorBrush(TableBgColors[idx]);
                    if (bd.Child is TextBlock tb)
                    {
                        tb.Foreground = selected
                            ? Brushes.White
                            : new SolidColorBrush(TableColors[idx]);
                    }
                }
            }
        }

        private void ShowSingleTable(int n)
        {
            if (pnlSingleTable == null) return;
            pnlSingleTable.Children.Clear();

            int idx = (n - 1) % TableColors.Length;
            var accentColor = TableColors[idx];
            var bgColor = TableBgColors[idx];

            borderSingleTable.Background = new SolidColorBrush(bgColor);
            borderSingleTable.BorderBrush = new SolidColorBrush(accentColor);
            borderSingleTable.BorderThickness = new Thickness(3);
            borderSingleTable.MinWidth = 400;

            // Title
            var title = new TextBlock
            {
                Text = $"Bảng nhân {n}",
                FontSize = 26,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(accentColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16),
                FontFamily = new FontFamily("Segoe UI"),
            };
            pnlSingleTable.Children.Add(title);

            // Table Grid: 10 rows for calculations, 7 columns for centered vertical alignment
            var tableGrid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Col 0: Left space
            tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Col 1: Factor 1
            tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Col 2: ×
            tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Col 3: Factor 2
            tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Col 4: =
            tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Col 5: Result
            tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Col 6: Right space

            for (int i = 1; i <= 10; i++)
            {
                int rIdx = i - 1;
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Alternating row background border spanning all 7 columns
                var rowBg = new Border
                {
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(16, 8, 16, 8),
                    Margin = new Thickness(0, 2, 0, 2),
                    Height = 44,
                    Background = i % 2 == 0
                        ? new SolidColorBrush(Color.FromArgb(30, accentColor.R, accentColor.G, accentColor.B))
                        : Brushes.Transparent,
                };
                Grid.SetRow(rowBg, rIdx);
                Grid.SetColumn(rowBg, 0);
                Grid.SetColumnSpan(rowBg, 7);
                tableGrid.Children.Add(rowBg);

                var txtF1 = new TextBlock
                {
                    Text = n.ToString(),
                    FontSize = 22,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(55, 71, 79)),
                    FontFamily = new FontFamily("Segoe UI"),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetRow(txtF1, rIdx);
                Grid.SetColumn(txtF1, 1);
                tableGrid.Children.Add(txtF1);

                var txtSign = new TextBlock
                {
                    Text = "×",
                    FontSize = 22,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(55, 71, 79)),
                    FontFamily = new FontFamily("Segoe UI"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 10, 0),
                };
                Grid.SetRow(txtSign, rIdx);
                Grid.SetColumn(txtSign, 2);
                tableGrid.Children.Add(txtSign);

                var txtF2 = new TextBlock
                {
                    Text = i.ToString(),
                    FontSize = 22,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(55, 71, 79)),
                    FontFamily = new FontFamily("Segoe UI"),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetRow(txtF2, rIdx);
                Grid.SetColumn(txtF2, 3);
                tableGrid.Children.Add(txtF2);

                var txtEq = new TextBlock
                {
                    Text = "=",
                    FontSize = 22,
                    Foreground = new SolidColorBrush(accentColor),
                    FontWeight = FontWeights.Bold,
                    FontFamily = new FontFamily("Segoe UI"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(12, 0, 12, 0),
                };
                Grid.SetRow(txtEq, rIdx);
                Grid.SetColumn(txtEq, 4);
                tableGrid.Children.Add(txtEq);

                var txtRes = new TextBlock
                {
                    Text = $"{n * i}",
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(accentColor),
                    FontFamily = new FontFamily("Segoe UI"),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetRow(txtRes, rIdx);
                Grid.SetColumn(txtRes, 5);
                tableGrid.Children.Add(txtRes);
            }
            pnlSingleTable.Children.Add(tableGrid);

            // Tip
            var tip = new TextBlock
            {
                Text = $"💡 Mẹo: {GetTip(n)}",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(90, 90, 90)),
                FontStyle = FontStyles.Italic,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(16, 16, 16, 0),
                FontFamily = new FontFamily("Segoe UI"),
            };
            pnlSingleTable.Children.Add(tip);
        }

        private static string GetTip(int n) => n switch
        {
            1 => "Số nào nhân 1 cũng bằng chính nó!",
            2 => "Nhân 2 = gấp đôi. 2,4,6,8,10... toàn số chẵn!",
            3 => "Quy luật: kết quả sau bằng kết quả trước cộng thêm 3 (3, 6, 9, 12...).",
            4 => "Nhân 4 = nhân 2 rồi nhân 2 lần nữa!",
            5 => "Kết quả luôn tận cùng bằng 0 hoặc 5!",
            6 => "Nhân 6 = nhân 3 rồi nhân 2!",
            7 => "Sử dụng tính chất giao hoán (ví dụ: 7 × 5 = 5 × 7 = 35) để nhớ dựa trên bảng nhân khác.",
            8 => "Nhân 8 = nhân 2 ba lần liên tiếp!",
            9 => "Tổng 2 chữ số kết quả luôn = 9 (9,18,27...)",
            10 => "Chỉ cần thêm số 0 vào sau!",
            _ => ""
        };

        // ═══════════════════════════════════════════════════════════
        //  XUẤT ẢNH NỀN TRẮNG
        // ═══════════════════════════════════════════════════════════

        private void ExportImage_Click(object sender, MouseButtonEventArgs e)
        {
            RenderWhiteTableImage();
            if (mainContentGrid != null) mainContentGrid.Visibility = Visibility.Collapsed;
            imageOverlay.Visibility = Visibility.Visible;
        }

        private void CopyImage_Click(object sender, MouseButtonEventArgs e)
        {
            if (_lastRenderedImage != null)
            {
                try { Clipboard.SetImage(_lastRenderedImage); }
                catch { }
            }
        }

        private void CloseImage_Click(object sender, MouseButtonEventArgs e)
        {
            imageOverlay.Visibility = Visibility.Collapsed;
            if (mainContentGrid != null) mainContentGrid.Visibility = Visibility.Visible;
        }

        private void RenderWhiteTableImage()
        {
            // Render all 10 tables as 2 rows × 5 columns
            int cardW = 220, cardH = 340, pad = 20, gap = 12;
            int cols = 5, rows = 2;
            int totalW = pad * 2 + cols * cardW + (cols - 1) * gap;
            int totalH = pad * 2 + rows * cardH + (rows - 1) * gap + 50; // +50 for title

            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, totalW, totalH));

                // Title
                var titleFt = MakeFormattedText("BẢNG CỬU CHƯƠNG NHÂN", 22, FontWeights.Bold,
                    Color.FromRgb(25, 118, 210));
                dc.DrawText(titleFt, new Point((totalW - titleFt.Width) / 2, pad));

                int startY = pad + 42;

                for (int n = 1; n <= 10; n++)
                {
                    int r = (n - 1) / cols;
                    int c = (n - 1) % cols;
                    double x = pad + c * (cardW + gap);
                    double y = startY + r * (cardH + gap);

                    int idx = (n - 1) % TableColors.Length;
                    var accent = TableColors[idx];
                    var bg = TableBgColors[idx];

                    // Card background
                    dc.DrawRoundedRectangle(new SolidColorBrush(bg),
                        new Pen(new SolidColorBrush(accent), 2),
                        new Rect(x, y, cardW, cardH), 8, 8);

                    // Header bar
                    dc.DrawRoundedRectangle(new SolidColorBrush(accent), null,
                        new Rect(x, y, cardW, 32), 8, 8);
                    // Fill corners
                    dc.DrawRectangle(new SolidColorBrush(accent), null,
                        new Rect(x, y + 16, cardW, 16));

                    var headerFt = MakeFormattedText($"Bảng nhân {n}", 14, FontWeights.Bold, Colors.White);
                    dc.DrawText(headerFt, new Point(x + (cardW - headerFt.Width) / 2, y + 7));

                    // Rows - Align perfectly in columns
                    for (int i = 1; i <= 10; i++)
                    {
                        double ry = y + 38 + (i - 1) * 29;
                        var color = Color.FromRgb(55, 71, 79);

                        // 1. First factor (n) - Right aligned at x + 55
                        var ftN = MakeFormattedText(n.ToString(), 13, FontWeights.SemiBold, color);
                        ftN.TextAlignment = TextAlignment.Right;
                        dc.DrawText(ftN, new Point(x + 55, ry));

                        // 2. Multiplication sign (×) - Center aligned at x + 70
                        var ftMul = MakeFormattedText("×", 13, FontWeights.SemiBold, color);
                        ftMul.TextAlignment = TextAlignment.Center;
                        dc.DrawText(ftMul, new Point(x + 70, ry));

                        // 3. Second factor (i) - Left aligned at x + 85
                        var ftI = MakeFormattedText(i.ToString(), 13, FontWeights.SemiBold, color);
                        ftI.TextAlignment = TextAlignment.Left;
                        dc.DrawText(ftI, new Point(x + 85, ry));

                        // 4. Equals sign (=) - Center aligned at x + 110
                        var ftEq = MakeFormattedText("=", 13, FontWeights.Bold, accent);
                        ftEq.TextAlignment = TextAlignment.Center;
                        dc.DrawText(ftEq, new Point(x + 110, ry));

                        // 5. Result (n * i) - Left aligned at x + 130
                        var ftRes = MakeFormattedText((n * i).ToString(), 13, FontWeights.Bold, accent);
                        ftRes.TextAlignment = TextAlignment.Left;
                        dc.DrawText(ftRes, new Point(x + 130, ry));
                    }
                }
            }

            var rtb = new RenderTargetBitmap(totalW, totalH, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            _lastRenderedImage = rtb;
            imgPreview.Source = rtb;
        }

        private static FormattedText MakeFormattedText(string text, double size, FontWeight weight, Color color)
        {
            return new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI, Arial"), FontStyles.Normal, weight, FontStretches.Normal),
                size,
                new SolidColorBrush(color),
                VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip);
        }

        // ═══════════════════════════════════════════════════════════
        //  FLASH CARDS — Luyện tập từng bảng
        // ═══════════════════════════════════════════════════════════

        private void InitFlashComboBox()
        {
            if (cboFlashTable == null) return;
            cboFlashTable.Items.Clear();
            cboFlashTable.Items.Add(new ComboBoxItem { Content = "Tất cả (1-10)" });
            for (int i = 1; i <= 10; i++)
                cboFlashTable.Items.Add(new ComboBoxItem { Content = $"Bảng nhân {i}" });
            cboFlashTable.SelectedIndex = 0;
        }

        private void FlashTableChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || cboFlashTable == null) return;
            _flashTableFilter = cboFlashTable.SelectedIndex; // 0=all, 1=×1, ..., 10=×10
            GenerateFlashCard();
        }

        private void GenerateFlashCard()
        {
            if (_flashTableFilter == 0)
            {
                _flashA = _rng.Next(1, 11);
            }
            else
            {
                _flashA = _flashTableFilter;
            }
            _flashB = _rng.Next(1, 11);
            _flashRevealed = false;

            if (txtFlashQuestion != null) txtFlashQuestion.Text = $"{_flashA} × {_flashB} = ?";
            if (txtFlashAnswer != null)
            {
                txtFlashAnswer.Text = "Nhấn để xem đáp án";
                txtFlashAnswer.Foreground = Brushes.Gray;
                txtFlashAnswer.FontSize = 16;
                txtFlashAnswer.FontWeight = FontWeights.Normal;
            }
        }

        private void FlashCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (!_flashRevealed)
            {
                txtFlashAnswer.Text = $"= {_flashA * _flashB}";
                txtFlashAnswer.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                txtFlashAnswer.FontSize = 28;
                txtFlashAnswer.FontWeight = FontWeights.Bold;
                _flashRevealed = true;
            }
            else
            {
                GenerateFlashCard();
            }
        }

        private void FlashNext_Click(object sender, RoutedEventArgs e) => GenerateFlashCard();
        private void FlashRandom_Click(object sender, RoutedEventArgs e) => GenerateFlashCard();

        // ═══════════════════════════════════════════════════════════
        //  QUIZ TỐC ĐỘ — Kiểm tra từng bảng
        // ═══════════════════════════════════════════════════════════

        private void InitQuizComboBox()
        {
            if (cboQuizTable == null) return;
            cboQuizTable.Items.Clear();
            cboQuizTable.Items.Add(new ComboBoxItem { Content = "Tất cả (1-10)" });
            for (int i = 1; i <= 10; i++)
                cboQuizTable.Items.Add(new ComboBoxItem { Content = $"Bảng nhân {i}" });
            cboQuizTable.SelectedIndex = 0;
        }

        private void QuizStart_Click(object sender, RoutedEventArgs e)
        {
            _quizTableFilter = cboQuizTable?.SelectedIndex ?? 0;
            _quizCorrect = 0;
            _quizTotal = 0;
            _quizActive = true;
            _isWaitingForNextQuestion = false;
            _quizStopwatch.Restart();

            if (_transitionTimer != null)
            {
                _transitionTimer.Stop();
                _transitionTimer = null;
            }

            _quizTimer.Start();

            txtQuizAnswer.IsEnabled = true;
            txtQuizAnswer.Visibility = Visibility.Visible;
            txtQuizAnswer.Focus();
            btnQuizStart.Content = "🔄 Chơi lại";

            if (cboQuizTable != null)
                cboQuizTable.IsEnabled = false;

            if (txtQuizInstruction != null)
                txtQuizInstruction.Visibility = Visibility.Visible;

            NextQuizQuestion();
        }

        private void NextQuizQuestion()
        {
            if (_quizTableFilter == 0)
                _quizA = _rng.Next(1, 11);
            else
                _quizA = _quizTableFilter;

            _quizB = _rng.Next(1, 11);
            txtQuizQuestion.Text = $"{_quizA} × {_quizB} = ?";
            txtQuizAnswer.Text = "";
            txtQuizAnswer.Focus();
            UpdateQuizScore();
        }

        private void QuizAnswer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SubmitQuizAnswer();
            }
        }

        private void SubmitQuizAnswer()
        {
            if (!_quizActive || _isWaitingForNextQuestion) return;
            if (string.IsNullOrWhiteSpace(txtQuizAnswer.Text)) return;

            if (int.TryParse(txtQuizAnswer.Text.Trim(), out int answer))
            {
                _quizTotal++;
                if (answer == _quizA * _quizB)
                {
                    _quizCorrect++;
                    txtQuizQuestion.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                }
                else
                {
                    txtQuizQuestion.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                    txtQuizQuestion.Text = $"{_quizA} × {_quizB} = {_quizA * _quizB}  (bạn: {answer})";
                }

                txtQuizAnswer.Text = ""; // Clear ngay để tránh double-trigger

                if (_quizTotal >= _quizTarget)
                {
                    _quizActive = false;
                    _quizStopwatch.Stop();
                    _quizTimer.Stop();

                    double elapsedSec = _quizStopwatch.Elapsed.TotalSeconds;
                    string medal = "";
                    if (_quizCorrect == 10 && elapsedSec < 15.0)
                        medal = "🥇 Huy chương Vàng";
                    else if (_quizCorrect >= 9 && elapsedSec < 20.0)
                        medal = "🥈 Huy chương Bạc";
                    else if (_quizCorrect >= 8 && elapsedSec < 30.0)
                        medal = "🥉 Huy chương Đồng";

                    if (!string.IsNullOrEmpty(medal))
                    {
                        txtQuizQuestion.Text = $"🎉 Hoàn thành! {_quizCorrect}/{_quizTarget} đúng trong {elapsedSec:F1}s\nBạn đạt: {medal}!";
                    }
                    else
                    {
                        txtQuizQuestion.Text = $"🎉 Hoàn thành! {_quizCorrect}/{_quizTarget} đúng trong {elapsedSec:F1}s";
                    }

                    txtQuizQuestion.Foreground = new SolidColorBrush(Color.FromRgb(26, 35, 126));
                    txtQuizAnswer.Visibility = Visibility.Collapsed;

                    if (txtQuizInstruction != null)
                        txtQuizInstruction.Visibility = Visibility.Collapsed;

                    if (cboQuizTable != null)
                        cboQuizTable.IsEnabled = true;

                    UpdateQuizScore();
                    return;
                }

                _isWaitingForNextQuestion = true;
                txtQuizAnswer.IsEnabled = false;

                _transitionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
                _transitionTimer.Tick += (_, _) =>
                {
                    _transitionTimer.Stop();
                    _transitionTimer = null;
                    _isWaitingForNextQuestion = false;
                    txtQuizAnswer.IsEnabled = true;
                    txtQuizQuestion.Foreground = new SolidColorBrush(Color.FromRgb(26, 35, 126));
                    NextQuizQuestion();
                };
                _transitionTimer.Start();
            }
        }

        private void UpdateQuizScore()
        {
            txtQuizScore.Text = $"Điểm: {_quizCorrect}/{_quizTotal}";
            txtQuizScore.Foreground = _quizCorrect == _quizTotal
                ? new SolidColorBrush(Color.FromRgb(46, 125, 50))
                : new SolidColorBrush(Color.FromRgb(230, 81, 0));
        }

        private void QuizAnswer_LostFocus(object sender, RoutedEventArgs e)
        {
            SubmitQuizAnswer();
        }

        private void SaveImage_Click(object sender, MouseButtonEventArgs e)
        {
            if (_lastRenderedImage == null) return;

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg",
                FileName = "BangCuuChuong.png"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (var fs = new System.IO.FileStream(dialog.FileName, System.IO.FileMode.Create))
                    {
                        BitmapEncoder encoder;
                        if (dialog.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                            dialog.FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                        {
                            encoder = new JpegBitmapEncoder();
                        }
                        else
                        {
                            encoder = new PngBitmapEncoder();
                        }
                        encoder.Frames.Add(BitmapFrame.Create(_lastRenderedImage));
                        encoder.Save(fs);
                    }
                    MessageBox.Show("Lưu ảnh thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể lưu ảnh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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
                        Icon = "🍬",
                        Title = isVN ? "Chia kẹo cho bạn" : "Bulk Grocery Shopping",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_multiplication_1_{suffix}.png",
                        Description = isVN 
                            ? "Phép nhân dùng để chia đều kẹo. Ví dụ: Chia cho 5 bạn, mỗi bạn 3 cái kẹo thì cần: 5 × 3 = 15 cái kẹo." 
                            : "Calculate total cost when buying multiple units of the same item (e.g. 5 boxes of milk at 12,000 VND each: 5 * 12,000 = 60,000 VND)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📅",
                        Title = isVN ? "Tính ngày trên lịch" : "Batch Baking & Cooking",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_multiplication_2_{suffix}.png",
                        Description = isVN 
                            ? "Mỗi tuần có 7 ngày. Để tính số ngày trong 4 tuần, ta lấy 7 × 4 = 28 ngày thay vì phải cộng lần lượt từng ngày." 
                            : "Scale up baking recipes for large parties by multiplying ingredients (e.g. triple flour amounts for 3 cakes)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🧸",
                        Title = isVN ? "Mua sắm đồ chơi" : "Floor Area Calculation",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_multiplication_3_{suffix}.png",
                        Description = isVN 
                            ? "Khi mua nhiều món đồ chơi cùng giá. Ví dụ: Mua 3 chiếc ô tô giá 10 nghìn đồng mỗi chiếc là 10 × 3 = 30 nghìn đồng." 
                            : "Determine square footage of rooms to buy tiles or carpet by multiplying length by width (e.g. 5m * 4m = 20 sqm)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🧱",
                        Title = isVN ? "Đếm gạch sàn nhà" : "Work hours & Wages",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_multiplication_4_{suffix}.png",
                        Description = isVN 
                            ? "Đếm gạch hoặc ô vuông xếp theo lưới. Ví dụ: Lưới gạch có 6 hàng, mỗi hàng 8 viên gạch là 6 × 8 = 48 viên gạch." 
                            : "Compute monthly salary by multiplying hourly wage by total hours worked (e.g. 40 hours * 50,000 VND/hour)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🏃",
                        Title = isVN ? "Đội hình học sinh" : "Classroom Seating Layout",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_multiplication_5_{suffix}.png",
                        Description = isVN 
                            ? "Tính nhanh số học sinh xếp hàng. Ví dụ: Có 4 hàng dọc, mỗi hàng có 10 học sinh đứng đều nhau là 4 × 10 = 40 học sinh." 
                            : "Find total student seating capacity by multiplying the number of rows by the number of chairs per row."
                    },
                    new PracticalAppItem
                    {
                        Icon = "👣",
                        Title = isVN ? "Đo khoảng cách" : "Travel Distance Estimation",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_multiplication_6_{suffix}.png",
                        Description = isVN 
                            ? "Đo quãng đường bằng bước chân. Ví dụ: Em đi được 8 bước chân, mỗi bước dài 50cm, quãng đường dài: 50 × 8 = 400cm." 
                            : "Estimate total distance traveled by multiplying speed by driving time (e.g. driving at 60 km/h for 3 hours: 60 * 3 = 180 km)."
                    }
                ,
                    new PracticalAppItem
                    {
                        Icon = "📦",
                        Title = isVN ? "Sắp xếp hàng hóa kho bãi" : "Warehouse Inventory Layout",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_multiplication_7_{suffix}.png",
                        Description = isVN 
                            ? "Tính nhanh tổng số lượng kiện hàng xếp trong kho bằng cách nhân số hàng, số cột và số lớp chồng lên nhau." 
                            : "Quickly compute total inventory units in a warehouse by multiplying rows, columns, and stacked layers."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🏫",
                        Title = isVN ? "Chia nhóm học tập trong lớp" : "Classroom Group Division",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_multiplication_8_{suffix}.png",
                        Description = isVN 
                            ? "Sử dụng phép nhân để ước tính nhanh số lượng tài liệu hoặc dụng cụ học tập cần chuẩn bị cho các nhóm thảo luận." 
                            : "Use multiplication to estimate total worksheets or materials needed for all collaborative learning groups."
                    }};

                practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for MultiplicationTool: {Err}", ex.Message);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewAllTables == null || viewSingleTable == null || viewFlash == null || viewQuiz == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewAllTables.Visibility = Visibility.Collapsed;
            viewSingleTable.Visibility = Visibility.Collapsed;
            viewFlash.Visibility = Visibility.Collapsed;
            viewQuiz.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewAllTables.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewSingleTable.Visibility = Visibility.Visible;
                    break;
                case 3:
                    viewFlash.Visibility = Visibility.Visible;
                    break;
                case 4:
                    viewQuiz.Visibility = Visibility.Visible;
                    break;
                case 5:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}