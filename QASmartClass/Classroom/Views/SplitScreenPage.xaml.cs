using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class SplitScreenPage : Page
    {
        private string _currentLayout = "1";
        private readonly List<System.Windows.Threading.DispatcherTimer> _activeTimers = new();

        // Content types that can be assigned to each panel
        private static readonly ContentOption[] ContentOptions =
        {
            new("🖥️", "Màn hình chính", "Hiển thị desktop hoặc ứng dụng đang mở", "#1976D2"),
            new("📚", "Bài giảng", "Trình chiếu bài giảng PowerPoint, PDF", "#2E7D32"),
            new("📷", "Camera", "Camera lớp học hoặc camera tài liệu", "#E65100"),
            new("🖼️", "Thư viện", "Hình ảnh, tài liệu từ thư viện tài nguyên", "#7B1FA2"),
            new("📊", "Quiz / Kiểm tra", "Hiển thị câu hỏi quiz thời gian thực", "#C62828"),
            new("🌐", "Trình duyệt Web", "Mở trang web, video YouTube...", "#00695C"),
            new("📝", "Bảng vẽ", "Canvas vẽ tự do, ghi chú bảng trắng", "#37474F"),
            new("⏱️", "Đồng hồ / Timer", "Đếm ngược, đồng hồ bấm giờ", "#F57F17"),
            new("👥", "Danh sách HS", "Hiển thị danh sách học sinh, điểm danh", "#1565C0"),
        };

        // Track what's assigned to each panel
        private readonly Dictionary<int, ContentOption> _panelAssignments = new();

        // Suggestion messages per layout
        private static readonly Dictionary<string, string> LayoutSuggestions = new()
        {
            ["1"] = "💡 Bố cục 1 màn hình: Phù hợp khi trình chiếu bài giảng toàn màn hình hoặc phát video cho cả lớp xem.",
            ["2"] = "💡 Bố cục đôi: Lý tưởng cho dạy song song — ví dụ bên trái là bài giảng, bên phải là bảng vẽ để giảng giải thêm, hoặc bên phải mở camera tài liệu.",
            ["3"] = "💡 Bố cục 1+2: Vùng chính cho bài giảng, 2 vùng phụ bên phải có thể gán camera + quiz, hoặc thư viện ảnh + timer đếm ngược.",
            ["4"] = "💡 Bố cục 2×2: Tốt cho hoạt động nhóm — mỗi vùng hiển thị bài làm của 1 nhóm, hoặc so sánh 4 nội dung khác nhau cùng lúc.",
            ["pip"] = "💡 Hình trong hình (PiP): Trình chiếu bài giảng toàn màn hình, góc nhỏ hiện camera giáo viên hoặc đồng hồ đếm ngược.",
        };

        public SplitScreenPage()
        {
            InitializeComponent();
            Loaded += (_, _) => ApplyLayout("1");
            Unloaded += (_, _) => { CleanupTimers(); ExitFullscreen(); };
        }

        private void CleanupTimers()
        {
            foreach (var timer in _activeTimers)
            {
                try { timer.Stop(); } catch { }
            }
            _activeTimers.Clear();
        }

        private void Layout_Click(object sender, MouseButtonEventArgs e)
        {
            var tag = (sender as FrameworkElement)?.Tag?.ToString() ?? "1";
            // Reset all border highlights
            foreach (var bd in new[] { b1, b2, b3, b4, bPip })
            {
                if (bd != null)
                {
                    bd.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                    bd.BorderThickness = new Thickness(1);
                    bd.Background = new SolidColorBrush(Colors.White);
                }
            }
            // Highlight selected
            var selected = tag switch { "1" => b1, "2" => b2, "3" => b3, "4" => b4, "pip" => bPip, _ => b1 };
            if (selected != null)
            {
                selected.BorderBrush = new SolidColorBrush(Color.FromRgb(25, 118, 210));
                selected.BorderThickness = new Thickness(2);
                selected.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
            }
            _currentLayout = tag;
            _panelAssignments.Clear();
            ApplyLayout(tag);
            Log.Information("SplitScreen layout: {Layout}", tag);
        }

        private void ApplyLayout(string layout)
        {
            CleanupTimers();
            previewArea.Children.Clear();
            previewArea.RowDefinitions.Clear();
            previewArea.ColumnDefinitions.Clear();

            // Update suggestion
            if (LayoutSuggestions.TryGetValue(layout, out var sug))
                txtSuggestion.Text = sug;

            switch (layout)
            {
                case "1":
                    AddCell(previewArea, 0, 0, 1, 1, 0, "🖥️ Màn hình chính", "Click để gán nội dung");
                    break;

                case "2":
                    // 3 columns: panel | splitter | panel
                    previewArea.ColumnDefinitions.Add(new ColumnDefinition());
                    previewArea.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    previewArea.ColumnDefinitions.Add(new ColumnDefinition());
                    AddCell(previewArea, 0, 0, 1, 1, 0, "🖥️ Vùng trái", "VD: Bài giảng");
                    AddGridSplitter(previewArea, 0, 1, 1, true);
                    AddCell(previewArea, 0, 2, 1, 1, 1, "📚 Vùng phải", "VD: Bảng vẽ, Camera");
                    break;

                case "3":
                    // 3 cols, 3 rows (with splitters)
                    previewArea.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
                    previewArea.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    previewArea.ColumnDefinitions.Add(new ColumnDefinition());
                    previewArea.RowDefinitions.Add(new RowDefinition());
                    previewArea.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    previewArea.RowDefinitions.Add(new RowDefinition());
                    AddCell(previewArea, 0, 0, 3, 1, 0, "🖥️ Vùng chính", "VD: Bài giảng toàn cỡ");
                    AddGridSplitter(previewArea, 0, 1, 3, true);  // vertical splitter spanning 3 rows
                    AddCell(previewArea, 0, 2, 1, 1, 1, "📷 Vùng phụ 1", "VD: Camera lớp");
                    AddHSplitter(previewArea, 1, 2);  // horizontal splitter in right column
                    AddCell(previewArea, 2, 2, 1, 1, 2, "📊 Vùng phụ 2", "VD: Quiz / Timer");
                    break;

                case "4":
                    // 3 cols, 3 rows
                    previewArea.ColumnDefinitions.Add(new ColumnDefinition());
                    previewArea.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    previewArea.ColumnDefinitions.Add(new ColumnDefinition());
                    previewArea.RowDefinitions.Add(new RowDefinition());
                    previewArea.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    previewArea.RowDefinitions.Add(new RowDefinition());
                    AddCell(previewArea, 0, 0, 1, 1, 0, "🖥️ Vùng 1", "VD: Bài giảng");
                    AddCell(previewArea, 0, 2, 1, 1, 1, "📚 Vùng 2", "VD: Thư viện");
                    AddCell(previewArea, 2, 0, 1, 1, 2, "📷 Vùng 3", "VD: Camera");
                    AddCell(previewArea, 2, 2, 1, 1, 3, "📊 Vùng 4", "VD: Quiz");
                    AddGridSplitter(previewArea, 0, 1, 3, true);   // vertical
                    AddHSplitter(previewArea, 1, 0, 3);            // horizontal spanning 3 cols
                    break;

                case "pip":
                    AddCell(previewArea, 0, 0, 1, 1, 0, "🖥️ Màn hình chính", "Nội dung toàn cỡ");
                    var pip = MakePanel(1, "📷 PiP", "VD: Camera GV", true);
                    pip.Width = 160;
                    pip.Height = 100;
                    pip.VerticalAlignment = VerticalAlignment.Bottom;
                    pip.HorizontalAlignment = HorizontalAlignment.Right;
                    pip.Margin = new Thickness(0, 0, 12, 12);
                    Grid.SetRow(pip, 0);
                    Grid.SetColumn(pip, 0);
                    previewArea.Children.Add(pip);
                    break;
            }
        }

        private void AddCell(Grid grid, int row, int col, int rowSpan, int colSpan, int panelIndex, string title, string hint)
        {
            var border = MakePanel(panelIndex, title, hint, false);
            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            Grid.SetRowSpan(border, Math.Max(1, rowSpan));
            Grid.SetColumnSpan(border, Math.Max(1, colSpan));
            grid.Children.Add(border);
        }

        /// <summary>Vertical GridSplitter</summary>
        private void AddGridSplitter(Grid grid, int row, int col, int rowSpan, bool vertical)
        {
            var splitter = new GridSplitter
            {
                Width = 6,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = new SolidColorBrush(Color.FromRgb(55, 71, 79)),
                Cursor = Cursors.SizeWE,
                ResizeDirection = GridResizeDirection.Columns
            };
            splitter.MouseEnter += (s, e) => splitter.Background = new SolidColorBrush(Color.FromRgb(25, 118, 210));
            splitter.MouseLeave += (s, e) => splitter.Background = new SolidColorBrush(Color.FromRgb(55, 71, 79));
            Grid.SetRow(splitter, row);
            Grid.SetColumn(splitter, col);
            if (rowSpan > 1) Grid.SetRowSpan(splitter, rowSpan);
            grid.Children.Add(splitter);
        }

        /// <summary>Horizontal GridSplitter</summary>
        private void AddHSplitter(Grid grid, int row, int col, int colSpan = 1)
        {
            var splitter = new GridSplitter
            {
                Height = 6,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                Background = new SolidColorBrush(Color.FromRgb(55, 71, 79)),
                Cursor = Cursors.SizeNS,
                ResizeDirection = GridResizeDirection.Rows
            };
            splitter.MouseEnter += (s, e) => splitter.Background = new SolidColorBrush(Color.FromRgb(25, 118, 210));
            splitter.MouseLeave += (s, e) => splitter.Background = new SolidColorBrush(Color.FromRgb(55, 71, 79));
            Grid.SetRow(splitter, row);
            Grid.SetColumn(splitter, col);
            if (colSpan > 1) Grid.SetColumnSpan(splitter, colSpan);
            grid.Children.Add(splitter);
        }
        // ═══════════════════════════════════════════════════════════
        //  IN-PLACE FULLSCREEN TOGGLE (preserves widget state)
        // ═══════════════════════════════════════════════════════════

        private bool _isFullscreen = false;
        private WindowState _savedWindowState;
        private WindowStyle _savedWindowStyle;
        private Thickness _savedRootMargin;
        private Thickness _savedPreviewMargin;
        private CornerRadius _savedPreviewCorner;
        private KeyEventHandler? _escHandler;

        private void Fullscreen_Click(object sender, RoutedEventArgs e)
        {
            if (_isFullscreen)
            {
                ExitFullscreen();
                return;
            }

            var wnd = Window.GetWindow(this);
            if (wnd == null) return;

            // Save state
            _savedWindowState = wnd.WindowState;
            _savedWindowStyle = wnd.WindowStyle;
            _savedRootMargin = rootGrid.Margin;
            _savedPreviewMargin = previewArea.Margin;
            _savedPreviewCorner = previewBorder.CornerRadius;

            // Hide UI chrome
            headerRow.Visibility = Visibility.Collapsed;
            layoutRow.Visibility = Visibility.Collapsed;
            suggestionRow.Visibility = Visibility.Collapsed;
            bottomRow.Visibility = Visibility.Collapsed;

            // Expand preview to fill
            rootGrid.Margin = new Thickness(0);
            previewArea.Margin = new Thickness(4);
            previewBorder.CornerRadius = new CornerRadius(0);

            // Add ESC overlay button
            var exitBtn = new Button
            {
                Content = "✕ Thoát (ESC)", FontSize = 10,
                Padding = new Thickness(10, 4, 10, 4),
                Background = new SolidColorBrush(Color.FromRgb(198, 40, 40)),
                Foreground = Brushes.White, BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand, Opacity = 0.85,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 8, 12, 0),
                Tag = "fsExitBtn"
            };
            exitBtn.Template = (ControlTemplate)System.Windows.Markup.XamlReader.Parse(
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Button'>" +
                "<Border Background='{TemplateBinding Background}' CornerRadius='5' Padding='{TemplateBinding Padding}'>" +
                "<ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>" +
                "</Border></ControlTemplate>");
            exitBtn.Click += (s2, e2) => ExitFullscreen();
            Panel.SetZIndex(exitBtn, 999);
            previewArea.Children.Add(exitBtn);

            // Maximize window
            wnd.WindowStyle = WindowStyle.None;
            wnd.WindowState = WindowState.Maximized;

            // ESC key handler
            _escHandler = (s2, e2) => { if (e2.Key == Key.Escape) ExitFullscreen(); };
            wnd.KeyDown += _escHandler;

            _isFullscreen = true;
            Log.Information("SplitScreen fullscreen entered");
        }

        private void ExitFullscreen()
        {
            if (!_isFullscreen) return;

            var wnd = Window.GetWindow(this);
            if (wnd == null) return;

            // Restore window
            wnd.WindowStyle = _savedWindowStyle;
            wnd.WindowState = _savedWindowState;

            // Remove ESC handler
            if (_escHandler != null)
            {
                wnd.KeyDown -= _escHandler;
                _escHandler = null;
            }

            // Restore UI chrome
            headerRow.Visibility = Visibility.Visible;
            layoutRow.Visibility = Visibility.Visible;
            suggestionRow.Visibility = Visibility.Visible;
            bottomRow.Visibility = Visibility.Visible;

            rootGrid.Margin = _savedRootMargin;
            previewArea.Margin = _savedPreviewMargin;
            previewBorder.CornerRadius = _savedPreviewCorner;

            // Remove exit button
            var toRemove = previewArea.Children.OfType<Button>().FirstOrDefault(b => b.Tag?.ToString() == "fsExitBtn");
            if (toRemove != null) previewArea.Children.Remove(toRemove);

            _isFullscreen = false;
            Log.Information("SplitScreen fullscreen exited");
        }

        private Border MakePanel(int panelIndex, string title, string hint, bool isPip)
        {
            UIElement content;

            // Check if already assigned → render real content
            if (_panelAssignments.TryGetValue(panelIndex, out var assigned))
            {
                content = isPip ? BuildPipContent(assigned) : BuildPanelContent(assigned);
            }
            else
            {
                // Empty panel placeholder
                var sp = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                sp.Children.Add(new TextBlock
                {
                    Text = title, FontSize = isPip ? 11 : 15,
                    Foreground = new SolidColorBrush(Color.FromRgb(144, 164, 174)),
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                sp.Children.Add(new TextBlock
                {
                    Text = hint, FontSize = isPip ? 8 : 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(96, 125, 139)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 0)
                });
                sp.Children.Add(new TextBlock
                {
                    Text = "🖱️ Click để gán", FontSize = isPip ? 8 : 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 8, 0, 0), FontStyle = FontStyles.Italic
                });
                content = sp;
            }

            var bgColor = _panelAssignments.ContainsKey(panelIndex)
                ? Color.FromRgb(21, 44, 68) : Color.FromRgb(21, 36, 58);

            var border = new Border
            {
                Background = new SolidColorBrush(bgColor),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(4),
                Child = content,
                Cursor = Cursors.Hand,
                ToolTip = _panelAssignments.ContainsKey(panelIndex)
                    ? $"Đã gán: {_panelAssignments[panelIndex].Name}\nClick chuột phải để đổi"
                    : "Click để chọn nội dung hiển thị"
            };

            // Right-click to reassign (so left-click doesn't conflict with interactive content)
            var capturedIdx = panelIndex;
            var ctx = new ContextMenu();
            var miChange = new MenuItem { Header = "🔄 Đổi nội dung" };
            miChange.Click += (s, e) => ShowContentPicker(capturedIdx);
            ctx.Items.Add(miChange);
            var miClear = new MenuItem { Header = "🗑️ Bỏ gán" };
            miClear.Click += (s, e) => { _panelAssignments.Remove(capturedIdx); ApplyLayout(_currentLayout); };
            ctx.Items.Add(miClear);
            border.ContextMenu = ctx;

            // Left click only opens picker if panel not assigned (so interactive content is not blocked)
            if (!_panelAssignments.ContainsKey(panelIndex))
                border.MouseLeftButtonDown += (s, e) => ShowContentPicker(capturedIdx);

            return border;
        }

        // ═══════════════════════════════════════════════════════════
        //  BUILD REAL CONTENT FOR EACH PANEL TYPE
        // ═══════════════════════════════════════════════════════════

        private UIElement BuildPipContent(ContentOption opt)
        {
            // Small PiP → just icon + name
            var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            sp.Children.Add(new TextBlock { Text = opt.Icon, FontSize = 18, HorizontalAlignment = HorizontalAlignment.Center });
            sp.Children.Add(new TextBlock { Text = opt.Name, FontSize = 9, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center });
            return sp;
        }

        private UIElement BuildPanelContent(ContentOption opt)
        {
            try
            {
                return opt.Name switch
                {
                    "Đồng hồ / Timer" => BuildTimerWidget(),
                    "Bảng vẽ" => BuildCanvasWidget(),
                    "Trình duyệt Web" => BuildWebWidget(),
                    "Danh sách HS" => BuildStudentListWidget(),
                    "Thư viện" => BuildLibraryWidget(),
                    "Quiz / Kiểm tra" => BuildQuizWidget(),
                    "Camera" => BuildCameraWidget(),
                    "Bài giảng" => BuildLessonWidget(),
                    _ => BuildGenericWidget(opt)
                };
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to build widget {Name}: {Err}", opt.Name, ex.Message);
                var errSp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                errSp.Children.Add(new TextBlock { Text = opt.Icon, FontSize = 28, HorizontalAlignment = HorizontalAlignment.Center });
                errSp.Children.Add(new TextBlock { Text = opt.Name, FontSize = 13, Foreground = Brushes.White, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center });
                errSp.Children.Add(new TextBlock { Text = "⚠️ Không thể tải widget", FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) });
                return errSp;
            }
        }

        // ── Timer Widget ──
        private UIElement BuildTimerWidget()
        {
            var grid = new Grid();
            var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

            sp.Children.Add(new TextBlock { Text = "⏱️", FontSize = 28, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 4) });

            var txtTime = new TextBlock
            {
                Text = "05:00", FontSize = 42, FontWeight = FontWeights.Bold,
                Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center,
                FontFamily = new FontFamily("Segoe UI")
            };
            sp.Children.Add(txtTime);

            var btnBar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };

            int totalSeconds = 300;
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _activeTimers.Add(timer);
            bool running = false;

            var btnStart = MakeSmallBtn("▶ Bắt đầu", "#4CAF50");
            var btnPause = MakeSmallBtn("⏸ Dừng", "#FF9800");
            var btnReset = MakeSmallBtn("↺ Reset", "#78909C");

            btnPause.Visibility = Visibility.Collapsed;

            timer.Tick += (s, e) =>
            {
                totalSeconds--;
                if (totalSeconds <= 0) { timer.Stop(); totalSeconds = 0; txtTime.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54)); }
                txtTime.Text = $"{totalSeconds / 60:D2}:{totalSeconds % 60:D2}";
            };

            btnStart.Click += (s, e) => { timer.Start(); running = true; btnStart.Visibility = Visibility.Collapsed; btnPause.Visibility = Visibility.Visible; };
            btnPause.Click += (s, e) => { timer.Stop(); running = false; btnStart.Visibility = Visibility.Visible; btnPause.Visibility = Visibility.Collapsed; };
            btnReset.Click += (s, e) => { timer.Stop(); running = false; totalSeconds = 300; txtTime.Text = "05:00"; txtTime.Foreground = Brushes.White; btnStart.Visibility = Visibility.Visible; btnPause.Visibility = Visibility.Collapsed; };

            btnBar.Children.Add(btnStart);
            btnBar.Children.Add(btnPause);
            btnBar.Children.Add(btnReset);
            sp.Children.Add(btnBar);

            // Preset buttons
            var presetBar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 0) };
            foreach (var sec in new[] { 60, 180, 300, 600 })
            {
                var label = sec < 60 ? $"{sec}s" : $"{sec / 60}p";
                var btn = MakeSmallBtn(label, "#37474F");
                var capturedSec = sec;
                btn.Click += (s, e) => { timer.Stop(); running = false; totalSeconds = capturedSec; txtTime.Text = $"{totalSeconds / 60:D2}:{totalSeconds % 60:D2}"; txtTime.Foreground = Brushes.White; btnStart.Visibility = Visibility.Visible; btnPause.Visibility = Visibility.Collapsed; };
                presetBar.Children.Add(btn);
            }
            sp.Children.Add(presetBar);
            grid.Children.Add(sp);
            return grid;
        }

        // ── Canvas / Whiteboard Widget ──
        private UIElement BuildCanvasWidget()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition());

            // Toolbar
            var toolbar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 4) };
            var ink = new System.Windows.Controls.InkCanvas
            {
                Background = Brushes.White,
                EditingMode = System.Windows.Controls.InkCanvasEditingMode.Ink,
                Margin = new Thickness(6)
            };
            ink.DefaultDrawingAttributes = new System.Windows.Ink.DrawingAttributes
            {
                Color = Colors.Black, Width = 3, Height = 3
            };

            var btnBlack = MakeSmallBtn("⚫", "#212121"); btnBlack.Click += (s, e) => { var da = ink.DefaultDrawingAttributes.Clone(); da.Color = Colors.Black; ink.DefaultDrawingAttributes = da; ink.EditingMode = System.Windows.Controls.InkCanvasEditingMode.Ink; };
            var btnRed = MakeSmallBtn("🔴", "#C62828"); btnRed.Click += (s, e) => { var da = ink.DefaultDrawingAttributes.Clone(); da.Color = Colors.Red; ink.DefaultDrawingAttributes = da; ink.EditingMode = System.Windows.Controls.InkCanvasEditingMode.Ink; };
            var btnBlue = MakeSmallBtn("🔵", "#1565C0"); btnBlue.Click += (s, e) => { var da = ink.DefaultDrawingAttributes.Clone(); da.Color = Colors.Blue; ink.DefaultDrawingAttributes = da; ink.EditingMode = System.Windows.Controls.InkCanvasEditingMode.Ink; };
            var btnEraser = MakeSmallBtn("🧹 Tẩy", "#78909C"); btnEraser.Click += (s, e) => ink.EditingMode = System.Windows.Controls.InkCanvasEditingMode.EraseByStroke;
            var btnClear = MakeSmallBtn("🗑️ Xóa", "#E65100"); btnClear.Click += (s, e) => ink.Strokes.Clear();

            toolbar.Children.Add(btnBlack);
            toolbar.Children.Add(btnRed);
            toolbar.Children.Add(btnBlue);
            toolbar.Children.Add(btnEraser);
            toolbar.Children.Add(btnClear);

            Grid.SetRow(toolbar, 0);
            Grid.SetRow(ink, 1);
            grid.Children.Add(toolbar);
            grid.Children.Add(ink);
            return grid;
        }

        // ── Web Browser Widget ──
        private UIElement BuildWebWidget()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition());

            // URL bar
            var urlBar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(6, 6, 6, 4) };
            var txtUrl = new TextBox
            {
                Text = "https://www.google.com", FontSize = 11,
                Padding = new Thickness(6, 3, 6, 3), MinWidth = 250,
                Background = new SolidColorBrush(Color.FromRgb(38, 50, 58)),
                Foreground = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(69, 90, 100)),
                CaretBrush = Brushes.White
            };
            var btnGo = MakeSmallBtn("🔍 Mở", "#1976D2");

            var wv = new Microsoft.Web.WebView2.Wpf.WebView2 { Margin = new Thickness(6, 0, 6, 6) };

            btnGo.Click += async (s, e) =>
            {
                try
                {
                    await wv.EnsureCoreWebView2Async();
                    var url = txtUrl.Text.Trim();
                    if (!url.StartsWith("http")) url = "https://" + url;
                    wv.CoreWebView2.Navigate(url);
                }
                catch { }
            };
            txtUrl.KeyDown += (s, e) => { if (e.Key == Key.Enter) btnGo.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent)); };

            urlBar.Children.Add(txtUrl);
            urlBar.Children.Add(btnGo);

            Grid.SetRow(urlBar, 0);
            Grid.SetRow(wv, 1);
            grid.Children.Add(urlBar);
            grid.Children.Add(wv);

            // Auto-load on render
            grid.Loaded += async (s, e) =>
            {
                try { await wv.EnsureCoreWebView2Async(); wv.CoreWebView2.Navigate("https://www.google.com"); } catch { }
            };

            return grid;
        }

        // ── Student List Widget ──
        private UIElement BuildStudentListWidget()
        {
            var mainGrid = new Grid { Margin = new Thickness(8) };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // header + stats
            mainGrid.RowDefinitions.Add(new RowDefinition());                            // student list

            // ── Header with stats ──
            var headerDp = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };

            // Load students from active roster
            List<QASmartClass.Data.Student> students;
            try
            {
                students = QASmartClass.Classroom.Services.RosterHelper.GetStudents()
                    .OrderBy(s => s.FullName.Split(' ').LastOrDefault() ?? s.FullName, StringComparer.CurrentCulture)
                    .ThenBy(s => s.FullName)
                    .ToList();
            }
            catch
            {
                students = new List<QASmartClass.Data.Student>();
            }

            int total = students.Count;
            int online = students.Count(s => s.IsOnline);

            // Stats on right
            var statsSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            DockPanel.SetDock(statsSp, Dock.Right);
            statsSp.Children.Add(new TextBlock { Text = $"🟢 {online}", FontSize = 9, Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)), Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center });
            statsSp.Children.Add(new TextBlock { Text = $"⚫ {total - online}", FontSize = 9, Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158)), VerticalAlignment = VerticalAlignment.Center });
            headerDp.Children.Add(statsSp);
            headerDp.Children.Add(new TextBlock
            {
                Text = $"👥 Danh sách học sinh ({total})", FontSize = 12,
                Foreground = Brushes.White, FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });
            Grid.SetRow(headerDp, 0);
            mainGrid.Children.Add(headerDp);

            // ── Student list ──
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var listSp = new StackPanel();
            Grid.SetRow(scroll, 1);

            if (students.Count == 0)
            {
                var emptyMsg = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 30, 0, 0) };
                emptyMsg.Children.Add(new TextBlock { Text = "👥", FontSize = 32, HorizontalAlignment = HorizontalAlignment.Center });
                emptyMsg.Children.Add(new TextBlock { Text = "Chưa có học sinh", FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(120, 144, 156)), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 4) });
                emptyMsg.Children.Add(new TextBlock { Text = "Thêm HS trong mục \"Học sinh\" ở menu bên trái", FontSize = 9, Foreground = new SolidColorBrush(Color.FromRgb(100, 120, 130)), HorizontalAlignment = HorizontalAlignment.Center, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center });
                listSp.Children.Add(emptyMsg);
            }
            else
            {
                int idx = 1;
                foreach (var s in students)
                {
                    var isOnline = s.IsOnline;
                    var row = new Border
                    {
                        Background = new SolidColorBrush(isOnline ? Color.FromRgb(21, 44, 68) : Color.FromRgb(42, 35, 35)),
                        CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 0, 0, 2),
                        Cursor = Cursors.Hand
                    };
                    row.MouseEnter += (se, ev) => row.Background = new SolidColorBrush(isOnline ? Color.FromRgb(30, 55, 80) : Color.FromRgb(55, 42, 42));
                    row.MouseLeave += (se, ev) => row.Background = new SolidColorBrush(isOnline ? Color.FromRgb(21, 44, 68) : Color.FromRgb(42, 35, 35));

                    var dp = new DockPanel();
                    // Status icon on right
                    var statusIcon = new TextBlock { Text = isOnline ? "🟢" : "⚫", FontSize = 10, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0) };
                    DockPanel.SetDock(statusIcon, Dock.Right);
                    dp.Children.Add(statusIcon);

                    // Student code on right
                    if (!string.IsNullOrEmpty(s.StudentCode))
                    {
                        var codeTb = new TextBlock { Text = s.StudentCode, FontSize = 8, Foreground = new SolidColorBrush(Color.FromRgb(120, 144, 156)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0) };
                        DockPanel.SetDock(codeTb, Dock.Right);
                        dp.Children.Add(codeTb);
                    }

                    // Name
                    dp.Children.Add(new TextBlock { Text = $"{idx}. {s.FullName}", FontSize = 11, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis });
                    row.Child = dp;
                    listSp.Children.Add(row);
                    idx++;
                }
            }

            scroll.Content = listSp;
            mainGrid.Children.Add(scroll);

            // Refresh button
            var btnRefresh = MakeSmallBtn("🔄 Cập nhật", "#37474F");
            btnRefresh.FontSize = 9;
            btnRefresh.HorizontalAlignment = HorizontalAlignment.Right;
            btnRefresh.VerticalAlignment = VerticalAlignment.Top;
            btnRefresh.Margin = new Thickness(0, 2, 0, 0);
            Panel.SetZIndex(btnRefresh, 10);
            Grid.SetRow(btnRefresh, 1);
            btnRefresh.Click += (s, e) =>
            {
                // Find parent and rebuild
                if (mainGrid.Parent is Border parentBorder)
                {
                    parentBorder.Child = BuildStudentListWidget();
                }
            };
            mainGrid.Children.Add(btnRefresh);

            return mainGrid;
        }

        // ── Library Widget (embedded file browser with collapsible panel) ──
        private UIElement BuildLibraryWidget()
        {
            var libPath = System.IO.Path.Combine(
                QASmartClass.Services.AppPaths.DocumentsDir, "Library");
            System.IO.Directory.CreateDirectory(libPath);

            var mainGrid = new Grid();
            // 3 columns: file list | splitter | preview
            var fileListCol = new ColumnDefinition { Width = new GridLength(180) };
            var splitterCol = new ColumnDefinition { Width = GridLength.Auto };
            var previewCol = new ColumnDefinition();
            mainGrid.ColumnDefinitions.Add(fileListCol);
            mainGrid.ColumnDefinitions.Add(splitterCol);
            mainGrid.ColumnDefinitions.Add(previewCol);

            // ── Left: File list panel ──
            var leftPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(15, 25, 40)),
                Name = "libLeftPanel"
            };
            var leftGrid = new Grid();
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            leftGrid.RowDefinitions.Add(new RowDefinition());

            // Header with toggle button
            var headerDp = new DockPanel { Margin = new Thickness(6, 6, 6, 4) };
            var btnToggle = MakeSmallBtn("◀ Ẩn", "#37474F");
            btnToggle.FontSize = 9;
            btnToggle.HorizontalAlignment = HorizontalAlignment.Right;
            DockPanel.SetDock(btnToggle, Dock.Right);
            headerDp.Children.Add(btnToggle);
            headerDp.Children.Add(new TextBlock
            {
                Text = "📚 Tài nguyên", FontSize = 11, FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center
            });
            Grid.SetRow(headerDp, 0);
            leftGrid.Children.Add(headerDp);

            var fileScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var fileListSp = new StackPanel();
            Grid.SetRow(fileScroll, 1);
            leftGrid.Children.Add(fileScroll);
            leftPanel.Child = leftGrid;

            // ── Splitter ──
            var splitter = new GridSplitter
            {
                Width = 5, HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = new SolidColorBrush(Color.FromRgb(55, 71, 79)),
                Cursor = Cursors.SizeWE, ResizeDirection = GridResizeDirection.Columns
            };
            splitter.MouseEnter += (s, e) => splitter.Background = new SolidColorBrush(Color.FromRgb(25, 118, 210));
            splitter.MouseLeave += (s, e) => splitter.Background = new SolidColorBrush(Color.FromRgb(55, 71, 79));

            // ── Right: Preview area ──
            var previewBorder = new Border { Background = new SolidColorBrush(Color.FromRgb(22, 38, 58)) };

            // Toggle button to show file list when collapsed
            var btnShow = MakeSmallBtn("▶ DS", "#1976D2");
            btnShow.FontSize = 9;
            btnShow.VerticalAlignment = VerticalAlignment.Top;
            btnShow.HorizontalAlignment = HorizontalAlignment.Left;
            btnShow.Margin = new Thickness(4, 4, 0, 0);
            btnShow.Visibility = Visibility.Collapsed;
            Panel.SetZIndex(btnShow, 10);

            var previewStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            previewStack.Children.Add(new TextBlock { Text = "📄", FontSize = 36, HorizontalAlignment = HorizontalAlignment.Center });
            previewStack.Children.Add(new TextBlock { Text = "Chọn file để xem trước", FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(120, 144, 156)), HorizontalAlignment = HorizontalAlignment.Center });

            var previewImage = new Image
            {
                Stretch = Stretch.Uniform, Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            var previewPdf = new Microsoft.Web.WebView2.Wpf.WebView2 { Visibility = Visibility.Collapsed };
            var previewTitle = new TextBlock
            {
                FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(176, 190, 197)),
                HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 4)
            };

            var previewGrid = new Grid();
            previewGrid.RowDefinitions.Add(new RowDefinition());
            previewGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            previewGrid.Children.Add(previewStack);
            previewGrid.Children.Add(previewImage);
            previewGrid.Children.Add(previewPdf);
            previewGrid.Children.Add(btnShow);
            Grid.SetRow(previewTitle, 1);
            previewGrid.Children.Add(previewTitle);
            previewBorder.Child = previewGrid;

            // ── Toggle collapse/expand ──
            bool isCollapsed = false;
            GridLength savedWidth = new GridLength(180);

            btnToggle.Click += (s, e) =>
            {
                savedWidth = fileListCol.Width;
                fileListCol.Width = new GridLength(0);
                splitterCol.Width = new GridLength(0);
                leftPanel.Visibility = Visibility.Collapsed;
                splitter.Visibility = Visibility.Collapsed;
                btnShow.Visibility = Visibility.Visible;
                isCollapsed = true;
            };

            btnShow.Click += (s, e) =>
            {
                fileListCol.Width = savedWidth.Value > 10 ? savedWidth : new GridLength(180);
                splitterCol.Width = GridLength.Auto;
                leftPanel.Visibility = Visibility.Visible;
                splitter.Visibility = Visibility.Visible;
                btnShow.Visibility = Visibility.Collapsed;
                isCollapsed = false;
            };

            // ── Load files ──
            try
            {
                var files = System.IO.Directory.GetFiles(libPath)
                    .Select(f => new System.IO.FileInfo(f))
                    .OrderBy(f => f.Name)
                    .ToArray();

                if (files.Length == 0)
                {
                    fileListSp.Children.Add(new TextBlock
                    {
                        Text = "Thư viện trống\nThêm file vào:\nDocuments/QA SmartClass/Library",
                        FontSize = 9, Foreground = new SolidColorBrush(Color.FromRgb(100, 120, 130)),
                        TextWrapping = TextWrapping.Wrap, Margin = new Thickness(6), TextAlignment = TextAlignment.Center
                    });
                }

                foreach (var fi in files)
                {
                    var ext = fi.Extension.ToLower();
                    var icon = ext switch
                    {
                        ".pdf" => "📄", ".jpg" or ".png" or ".jpeg" or ".bmp" => "🖼️",
                        ".mp4" or ".avi" => "🎬", ".docx" or ".doc" => "📝",
                        ".pptx" or ".ppt" => "📋", ".xlsx" or ".xls" => "📊", _ => "📄"
                    };
                    var sizeStr = fi.Length < 1048576 ? $"{fi.Length / 1024.0:F0} KB" : $"{fi.Length / 1048576.0:F1} MB";

                    var row = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(21, 36, 54)),
                        CornerRadius = new CornerRadius(3), Padding = new Thickness(5, 3, 5, 3),
                        Margin = new Thickness(3, 0, 3, 2), Cursor = Cursors.Hand
                    };
                    var dp = new DockPanel();
                    dp.Children.Add(new TextBlock
                    {
                        Text = sizeStr, FontSize = 7, Foreground = new SolidColorBrush(Color.FromRgb(120, 140, 150)),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    DockPanel.SetDock(dp.Children[0], Dock.Right);

                    var nameSp = new StackPanel { Orientation = Orientation.Horizontal };
                    nameSp.Children.Add(new TextBlock { Text = icon, FontSize = 10, Margin = new Thickness(0, 0, 3, 0), VerticalAlignment = VerticalAlignment.Center });
                    nameSp.Children.Add(new TextBlock
                    {
                        Text = fi.Name, FontSize = 9, Foreground = Brushes.White,
                        TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 110,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    dp.Children.Add(nameSp);
                    row.Child = dp;

                    var capturedPath = fi.FullName;
                    var capturedExt = ext;
                    var capturedName = fi.Name;
                    var capturedIcon = icon;

                    row.MouseEnter += (s, e) => row.Background = new SolidColorBrush(Color.FromRgb(30, 52, 72));
                    row.MouseLeave += (s, e) => row.Background = new SolidColorBrush(Color.FromRgb(21, 36, 54));

                    row.MouseLeftButtonDown += async (s, e) =>
                    {
                        previewTitle.Text = $"📄 {capturedName}";
                        if (capturedExt is ".jpg" or ".png" or ".jpeg" or ".bmp")
                        {
                            try
                            {
                                var bmp = new System.Windows.Media.Imaging.BitmapImage();
                                bmp.BeginInit();
                                bmp.UriSource = new Uri(capturedPath);
                                bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                                bmp.EndInit();
                                previewImage.Source = bmp;
                                previewImage.Visibility = Visibility.Visible;
                                previewPdf.Visibility = Visibility.Collapsed;
                                previewStack.Visibility = Visibility.Collapsed;
                            }
                            catch { }
                        }
                        else if (capturedExt == ".pdf")
                        {
                            try
                            {
                                previewImage.Visibility = Visibility.Collapsed;
                                previewPdf.Visibility = Visibility.Visible;
                                previewStack.Visibility = Visibility.Collapsed;
                                await previewPdf.EnsureCoreWebView2Async();
                                previewPdf.CoreWebView2.Navigate(new Uri(capturedPath).AbsoluteUri);
                            }
                            catch { }
                        }
                        else
                        {
                            previewImage.Visibility = Visibility.Collapsed;
                            previewPdf.Visibility = Visibility.Collapsed;
                            previewStack.Visibility = Visibility.Visible;
                            ((TextBlock)previewStack.Children[0]).Text = capturedIcon;
                            ((TextBlock)previewStack.Children[1]).Text = $"{capturedName}\nClick đôi để mở";
                        }

                        // Auto-collapse file list after selecting
                        if (!isCollapsed)
                            btnToggle.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                    };

                    fileListSp.Children.Add(row);
                }
            }
            catch { }

            fileScroll.Content = fileListSp;

            Grid.SetColumn(leftPanel, 0);
            Grid.SetColumn(splitter, 1);
            Grid.SetColumn(previewBorder, 2);
            mainGrid.Children.Add(leftPanel);
            mainGrid.Children.Add(splitter);
            mainGrid.Children.Add(previewBorder);

            return mainGrid;
        }

        // ── Quiz Widget ──
        // ── Quiz Widget (interactive quiz with teacher input) ──
        private UIElement BuildQuizWidget()
        {
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // header + mode tabs
            mainGrid.RowDefinitions.Add(new RowDefinition());                            // content

            // ── Header bar ──
            var headerDp = new DockPanel { Margin = new Thickness(8, 6, 8, 4) };
            headerDp.Children.Add(new TextBlock
            {
                Text = "📊 Quiz / Kiểm tra", FontSize = 12, FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center
            });
            Grid.SetRow(headerDp, 0);
            mainGrid.Children.Add(headerDp);

            // ── Content area ──
            var contentBorder = new Border { Margin = new Thickness(6, 0, 6, 6) };
            Grid.SetRow(contentBorder, 1);
            mainGrid.Children.Add(contentBorder);

            // State
            var questions = new List<(string Question, string[] Options, int CorrectIndex)>();
            int currentQ = 0;

            // ── Build "Create quiz" view ──
            var createSp = new StackPanel { Margin = new Thickness(4) };
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };

            var txtQuestion = new TextBox
            {
                Text = "", FontSize = 11, Height = 50, AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap, Background = new SolidColorBrush(Color.FromRgb(21, 36, 54)),
                Foreground = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(55, 71, 79)),
                BorderThickness = new Thickness(1), Padding = new Thickness(6, 4, 6, 4),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            TextBoxHelper_SetPlaceholder(txtQuestion, "Nhập câu hỏi...");

            createSp.Children.Add(new TextBlock { Text = "📝 Câu hỏi:", FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(144, 164, 174)), Margin = new Thickness(0, 0, 0, 2) });
            createSp.Children.Add(txtQuestion);

            // Answer inputs (4 default, can be A-F)
            var answerBoxes = new List<TextBox>();
            var correctRadios = new List<System.Windows.Controls.RadioButton>();
            var labels = new[] { "A", "B", "C", "D", "E", "F" };
            int answerCount = 4;

            var answersSp = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
            createSp.Children.Add(new TextBlock { Text = "📋 Đáp án (chọn ⦿ = đúng):", FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(144, 164, 174)), Margin = new Thickness(0, 6, 0, 2) });
            createSp.Children.Add(answersSp);

            for (int i = 0; i < answerCount; i++)
            {
                var rowDp = new DockPanel { Margin = new Thickness(0, 0, 0, 3) };
                var rb = new System.Windows.Controls.RadioButton
                {
                    GroupName = "correct", Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0)
                };
                if (i == 0) rb.IsChecked = true;
                correctRadios.Add(rb);
                DockPanel.SetDock(rb, Dock.Left);
                rowDp.Children.Add(rb);

                var labelTb = new TextBlock
                {
                    Text = $"{labels[i]}.", FontSize = 10, Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold, Width = 16, VerticalAlignment = VerticalAlignment.Center
                };
                DockPanel.SetDock(labelTb, Dock.Left);
                rowDp.Children.Add(labelTb);

                var tb = new TextBox
                {
                    FontSize = 10, Height = 22, Background = new SolidColorBrush(Color.FromRgb(21, 36, 54)),
                    Foreground = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(55, 71, 79)),
                    BorderThickness = new Thickness(1), Padding = new Thickness(4, 1, 4, 1)
                };
                TextBoxHelper_SetPlaceholder(tb, $"Đáp án {labels[i]}...");
                answerBoxes.Add(tb);
                rowDp.Children.Add(tb);
                answersSp.Children.Add(rowDp);
            }

            // Buttons: Add question + Start quiz
            var btnBar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0), HorizontalAlignment = HorizontalAlignment.Center };
            var btnAdd = MakeSmallBtn("➕ Thêm câu hỏi", "#1976D2");
            btnAdd.FontSize = 10;
            var btnStart = MakeSmallBtn("▶ Bắt đầu Quiz", "#2E7D32");
            btnStart.FontSize = 10;
            btnStart.Margin = new Thickness(6, 0, 0, 0);
            var btnLoadBank = MakeSmallBtn("📦 Từ ngân hàng", "#7B1FA2");
            btnLoadBank.FontSize = 10;
            btnLoadBank.Margin = new Thickness(6, 0, 0, 0);
            btnBar.Children.Add(btnAdd);
            btnBar.Children.Add(btnStart);
            btnBar.Children.Add(btnLoadBank);
            createSp.Children.Add(btnBar);

            var statusTb = new TextBlock
            {
                FontSize = 9, Foreground = new SolidColorBrush(Color.FromRgb(120, 144, 156)),
                Margin = new Thickness(0, 6, 0, 0), HorizontalAlignment = HorizontalAlignment.Center
            };
            createSp.Children.Add(statusTb);

            scroll.Content = createSp;
            contentBorder.Child = scroll;

            // ── Quiz display view (shown after Start) ──
            UIElement BuildQuizDisplay()
            {
                if (questions.Count == 0) return new TextBlock { Text = "Chưa có câu hỏi", Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center };

                var q = questions[currentQ];
                var dsp = new StackPanel { Margin = new Thickness(8), MaxWidth = 500, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

                // Question counter
                dsp.Children.Add(new TextBlock
                {
                    Text = $"Câu {currentQ + 1}/{questions.Count}", FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210)), FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 6)
                });

                // Question text
                dsp.Children.Add(new TextBlock
                {
                    Text = q.Question, FontSize = 13, Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center,
                    FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 12)
                });

                // Answer options
                for (int i = 0; i < q.Options.Length; i++)
                {
                    var isCorrect = (i == q.CorrectIndex);
                    var optBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(21, 44, 68)),
                        CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 7, 10, 7),
                        Margin = new Thickness(0, 0, 0, 4), Cursor = Cursors.Hand,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(55, 71, 79)), BorderThickness = new Thickness(1)
                    };
                    optBorder.Child = new TextBlock { Text = $"{labels[i]}. {q.Options[i]}", FontSize = 11, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap };

                    var capturedCorrect = isCorrect;
                    optBorder.MouseLeftButtonDown += (s, e) =>
                    {
                        optBorder.Background = new SolidColorBrush(capturedCorrect ? Color.FromRgb(27, 94, 32) : Color.FromRgb(183, 28, 28));
                        optBorder.BorderBrush = new SolidColorBrush(capturedCorrect ? Color.FromRgb(76, 175, 80) : Color.FromRgb(244, 67, 54));
                    };
                    dsp.Children.Add(optBorder);
                }

                // Nav buttons
                var navBar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
                if (currentQ > 0)
                {
                    var btnPrev = MakeSmallBtn("◀ Trước", "#37474F"); btnPrev.FontSize = 9;
                    btnPrev.Click += (s, e) => { currentQ--; contentBorder.Child = BuildQuizDisplay(); };
                    navBar.Children.Add(btnPrev);
                }
                if (currentQ < questions.Count - 1)
                {
                    var btnNext = MakeSmallBtn("Sau ▶", "#1976D2"); btnNext.FontSize = 9;
                    btnNext.Margin = new Thickness(6, 0, 0, 0);
                    btnNext.Click += (s, e) => { currentQ++; contentBorder.Child = BuildQuizDisplay(); };
                    navBar.Children.Add(btnNext);
                }
                var btnEdit = MakeSmallBtn("✏️ Sửa", "#F57F17"); btnEdit.FontSize = 9;
                btnEdit.Margin = new Thickness(6, 0, 0, 0);
                btnEdit.Click += (s, e) => { contentBorder.Child = scroll; };
                navBar.Children.Add(btnEdit);
                dsp.Children.Add(navBar);

                return dsp;
            }

            // ── Button handlers ──
            btnAdd.Click += (s, e) =>
            {
                var qText = txtQuestion.Text.Trim();
                if (string.IsNullOrEmpty(qText)) return;

                var opts = answerBoxes.Select(tb => tb.Text.Trim()).Where(t => t.Length > 0).ToArray();
                if (opts.Length < 2) { statusTb.Text = "⚠️ Cần ít nhất 2 đáp án"; return; }

                int correctIdx = 0;
                for (int i = 0; i < correctRadios.Count; i++)
                    if (correctRadios[i].IsChecked == true) { correctIdx = i; break; }

                questions.Add((qText, opts, correctIdx));
                statusTb.Text = $"✅ Đã thêm! Tổng: {questions.Count} câu";

                // Clear for next question
                txtQuestion.Clear();
                foreach (var tb in answerBoxes) tb.Clear();
                if (correctRadios.Count > 0) correctRadios[0].IsChecked = true;
                txtQuestion.Focus();
            };

            btnStart.Click += (s, e) =>
            {
                // Also add current if not empty
                var qText = txtQuestion.Text.Trim();
                if (!string.IsNullOrEmpty(qText))
                {
                    var opts = answerBoxes.Select(tb => tb.Text.Trim()).Where(t => t.Length > 0).ToArray();
                    if (opts.Length >= 2)
                    {
                        int correctIdx = 0;
                        for (int i = 0; i < correctRadios.Count; i++)
                            if (correctRadios[i].IsChecked == true) { correctIdx = i; break; }
                        questions.Add((qText, opts, correctIdx));
                    }
                }

                if (questions.Count == 0) { statusTb.Text = "⚠️ Chưa có câu hỏi nào!"; return; }
                currentQ = 0;
                contentBorder.Child = BuildQuizDisplay();
            };

            btnLoadBank.Click += (s, e) =>
            {
                try
                {
                    using var db = new QASmartClass.Data.AppDbContext();
                    var bankItems = db.QuestionBankItems
                        .OrderByDescending(q => q.CreatedAt)
                        .Take(20)
                        .ToList();

                    if (bankItems.Count == 0)
                    {
                        statusTb.Text = "📦 Ngân hàng câu hỏi trống. Hãy thêm câu hỏi trong mục Ngân hàng CQ.";
                        return;
                    }

                    questions.Clear();
                    foreach (var item in bankItems)
                    {
                        try
                        {
                            var options = System.Text.Json.JsonSerializer.Deserialize<string[]>(item.OptionsJson) ?? Array.Empty<string>();
                            if (options.Length < 2) continue;

                            int correctIdx = 0;
                            for (int i = 0; i < options.Length; i++)
                                if (options[i] == item.CorrectAnswer) { correctIdx = i; break; }

                            questions.Add((item.Content, options, correctIdx));
                        }
                        catch { }
                    }

                    if (questions.Count > 0)
                    {
                        statusTb.Text = $"📦 Đã tải {questions.Count} câu từ ngân hàng!";
                        currentQ = 0;
                        contentBorder.Child = BuildQuizDisplay();
                    }
                    else
                        statusTb.Text = "⚠️ Không tìm thấy câu hỏi phù hợp.";
                }
                catch (Exception ex)
                {
                    statusTb.Text = $"⚠️ Lỗi: {ex.Message}";
                }
            };

            return mainGrid;
        }

        /// <summary>Simple placeholder behavior for TextBox</summary>
        private static void TextBoxHelper_SetPlaceholder(TextBox tb, string placeholder)
        {
            tb.Tag = placeholder;
            tb.Foreground = new SolidColorBrush(Color.FromRgb(120, 130, 140));
            tb.Text = placeholder;
            tb.GotFocus += (s, e) => { if (tb.Text == (string)tb.Tag) { tb.Text = ""; tb.Foreground = Brushes.White; } };
            tb.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(tb.Text)) { tb.Text = (string)tb.Tag; tb.Foreground = new SolidColorBrush(Color.FromRgb(120, 130, 140)); } };
        }

        // ── Camera Widget ──
        private UIElement BuildCameraWidget()
        {
            var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            sp.Children.Add(new TextBlock { Text = "📷", FontSize = 48, HorizontalAlignment = HorizontalAlignment.Center });
            sp.Children.Add(new TextBlock { Text = "Camera lớp học", FontSize = 14, Foreground = Brushes.White, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 4) });
            sp.Children.Add(new TextBlock { Text = "Đang chờ kết nối camera...", FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(255, 183, 77)), HorizontalAlignment = HorizontalAlignment.Center, FontStyle = FontStyles.Italic });

            // Simulated loading animation
            var progress = new ProgressBar
            {
                IsIndeterminate = true, Width = 120, Height = 3,
                Margin = new Thickness(0, 8, 0, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0))
            };
            sp.Children.Add(progress);
            return sp;
        }

        // ── Lesson / Slideshow Widget ──
        private UIElement BuildLessonWidget()
        {
            var grid = new Grid();
            var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, MaxWidth = 400 };
            sp.Children.Add(new TextBlock { Text = "📚", FontSize = 36, HorizontalAlignment = HorizontalAlignment.Center });
            sp.Children.Add(new TextBlock { Text = "Bài giảng", FontSize = 14, Foreground = Brushes.White, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 8) });

            // Sample recent lessons
            foreach (var lesson in new[] { "Toán 10 — Chương 3: Phương trình bậc hai", "Vật Lý 11 — Điện trường", "Hóa Học 10 — Bảng tuần hoàn" })
            {
                var row = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(21, 44, 68)),
                    CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 0, 0, 4), Cursor = Cursors.Hand,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(55, 71, 79)), BorderThickness = new Thickness(1)
                };
                row.Child = new TextBlock { Text = $"📋 {lesson}", FontSize = 11, Foreground = Brushes.White };
                row.MouseEnter += (s, e) => row.Background = new SolidColorBrush(Color.FromRgb(30, 60, 90));
                row.MouseLeave += (s, e) => row.Background = new SolidColorBrush(Color.FromRgb(21, 44, 68));
                sp.Children.Add(row);
            }

            grid.Children.Add(sp);
            return grid;
        }

        // ── Generic Widget (fallback) ──
        private UIElement BuildGenericWidget(ContentOption opt)
        {
            var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            sp.Children.Add(new TextBlock { Text = opt.Icon, FontSize = 36, HorizontalAlignment = HorizontalAlignment.Center });
            sp.Children.Add(new TextBlock { Text = opt.Name, FontSize = 14, Foreground = Brushes.White, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 4) });
            sp.Children.Add(new TextBlock { Text = opt.Description, FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(144, 164, 174)), HorizontalAlignment = HorizontalAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxWidth = 250, TextAlignment = TextAlignment.Center });
            return sp;
        }

        private static Button MakeSmallBtn(string text, string bgHex)
        {
            var bg = (Color)ColorConverter.ConvertFromString(bgHex);
            var btn = new Button
            {
                Content = text, FontSize = 10, Padding = new Thickness(8, 3, 8, 3),
                Background = new SolidColorBrush(bg), Foreground = Brushes.White,
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                Margin = new Thickness(2, 0, 2, 0)
            };
            btn.Template = (ControlTemplate)System.Windows.Markup.XamlReader.Parse(
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Button'>" +
                "<Border x:Name='b' Background='{TemplateBinding Background}' CornerRadius='4' Padding='{TemplateBinding Padding}'>" +
                "<ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>" +
                "</Border></ControlTemplate>");
            return btn;
        }

        // ═══════════════════════════════════════════════════════════
        //  CONTENT PICKER (assign content to a panel)
        // ═══════════════════════════════════════════════════════════

        private void ShowContentPicker(int panelIndex)
        {
            var dlg = new Window
            {
                Title = "🔲 Chọn nội dung hiển thị",
                Width = 420, Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 250))
            };

            var mainSp = new StackPanel { Margin = new Thickness(16) };
            mainSp.Children.Add(new TextBlock
            {
                Text = $"Chọn nội dung cho vùng #{panelIndex + 1}",
                FontSize = 14, FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 12)
            });

            var scroll = new ScrollViewer { MaxHeight = 380, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var listSp = new StackPanel();

            foreach (var opt in ContentOptions)
            {
                var optCapture = opt;
                var isAssigned = _panelAssignments.TryGetValue(panelIndex, out var current) && current == opt;

                var row = new Border
                {
                    Background = isAssigned ? new SolidColorBrush(Color.FromRgb(227, 242, 253)) : Brushes.White,
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 10, 12, 10),
                    Margin = new Thickness(0, 0, 0, 4),
                    Cursor = Cursors.Hand,
                    BorderBrush = isAssigned ? new SolidColorBrush(Color.FromRgb(25, 118, 210))
                        : new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    BorderThickness = new Thickness(isAssigned ? 2 : 1)
                };

                var dp = new DockPanel();

                // Assign button right
                var assignBtn = new TextBlock
                {
                    Text = isAssigned ? "✅ Đã gán" : "▶ Chọn",
                    FontSize = 11, VerticalAlignment = VerticalAlignment.Center,
                    Foreground = isAssigned ? new SolidColorBrush(Color.FromRgb(46, 125, 50))
                        : new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                    FontWeight = FontWeights.SemiBold
                };
                DockPanel.SetDock(assignBtn, Dock.Right);
                dp.Children.Add(assignBtn);

                // Icon + text left
                var leftSp = new StackPanel { Orientation = Orientation.Horizontal };
                var accentBrush = (Brush)new BrushConverter().ConvertFromString(opt.AccentColor)!;
                leftSp.Children.Add(new Border
                {
                    Width = 36, Height = 36, CornerRadius = new CornerRadius(8),
                    Background = accentBrush, Margin = new Thickness(0, 0, 10, 0),
                    Child = new TextBlock
                    {
                        Text = opt.Icon, FontSize = 18,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                });
                var textSp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                textSp.Children.Add(new TextBlock { Text = opt.Name, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)) });
                textSp.Children.Add(new TextBlock { Text = opt.Description, FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)) });
                leftSp.Children.Add(textSp);
                dp.Children.Add(leftSp);

                row.Child = dp;

                row.MouseEnter += (s, e) => { if (!isAssigned) row.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)); };
                row.MouseLeave += (s, e) => { if (!isAssigned) row.Background = Brushes.White; };
                row.MouseLeftButtonDown += (s, e) =>
                {
                    _panelAssignments[panelIndex] = optCapture;
                    dlg.Close();
                    ApplyLayout(_currentLayout);
                };

                listSp.Children.Add(row);
            }

            // "Clear" option
            var clearRow = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 243, 224)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 8, 0, 0),
                Cursor = Cursors.Hand,
                BorderBrush = new SolidColorBrush(Color.FromRgb(255, 183, 77)),
                BorderThickness = new Thickness(1)
            };
            clearRow.Child = new TextBlock
            {
                Text = "🗑️ Bỏ gán — để trống vùng này",
                FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0)),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            clearRow.MouseLeftButtonDown += (s, e) =>
            {
                _panelAssignments.Remove(panelIndex);
                dlg.Close();
                ApplyLayout(_currentLayout);
            };
            listSp.Children.Add(clearRow);

            scroll.Content = listSp;
            mainSp.Children.Add(scroll);
            dlg.Content = mainSp;
            dlg.ShowDialog();
        }

        // ═══════════════════════════════════════════════════════════
        //  APPLY TO SCREEN
        // ═══════════════════════════════════════════════════════════

        private void ApplyToScreen_Click(object sender, RoutedEventArgs e)
        {
            if (_panelAssignments.Count == 0)
            {
                MessageBox.Show(
                    "⚠️ Chưa gán nội dung cho vùng nào!\n\n" +
                    "Hướng dẫn:\n" +
                    "1. Chọn bố cục (1, 2, 3, 4 hoặc PiP)\n" +
                    "2. Click vào từng vùng để gán nội dung\n" +
                    "3. Nhấn '✅ Áp dụng bố cục' để kích hoạt",
                    "Chưa gán nội dung", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Open fullscreen window with real content
            var fsWnd = new Window
            {
                Title = "QA SmartClass — Chia màn hình",
                WindowState = WindowState.Maximized,
                WindowStyle = WindowStyle.None,
                Background = new SolidColorBrush(Color.FromRgb(13, 27, 42)),
                AllowsTransparency = false
            };

            var mainGrid = new Grid();

            // Content area
            var contentGrid = new Grid { Margin = new Thickness(2) };
            BuildFullscreenLayout(contentGrid, _currentLayout);
            mainGrid.Children.Add(contentGrid);

            // Top-right close button
            var closeBar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 6, 12, 0)
            };
            Panel.SetZIndex(closeBar, 100);

            var layoutLabel = new TextBlock
            {
                Text = $"📐 {_currentLayout switch { "1" => "1 màn hình", "2" => "Đôi", "3" => "1+2 phụ", "4" => "2×2", "pip" => "PiP", _ => _currentLayout }}",
                FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(144, 164, 174)),
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0)
            };
            closeBar.Children.Add(layoutLabel);

            var closeBtn = new Button
            {
                Content = "✕ Thoát (ESC)", FontSize = 11,
                Padding = new Thickness(14, 6, 14, 6),
                Background = new SolidColorBrush(Color.FromRgb(198, 40, 40)),
                Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand
            };
            closeBtn.Template = (ControlTemplate)System.Windows.Markup.XamlReader.Parse(
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Button'>" +
                "<Border Background='{TemplateBinding Background}' CornerRadius='6' Padding='{TemplateBinding Padding}'>" +
                "<ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>" +
                "</Border></ControlTemplate>");
            closeBtn.Click += (s2, e2) => fsWnd.Close();
            closeBar.Children.Add(closeBtn);
            mainGrid.Children.Add(closeBar);

            // ESC to close
            fsWnd.KeyDown += (s2, e2) => { if (e2.Key == Key.Escape) fsWnd.Close(); };

            fsWnd.Content = mainGrid;
            fsWnd.ShowDialog();

            Log.Information("SplitScreen fullscreen closed: {Layout}", _currentLayout);
        }

        private void BuildFullscreenLayout(Grid grid, string layout)
        {
            grid.Children.Clear();
            grid.RowDefinitions.Clear();
            grid.ColumnDefinitions.Clear();

            switch (layout)
            {
                case "1":
                    AddFullscreenCell(grid, 0, 0, 1, 1, 0);
                    break;
                case "2":
                    grid.ColumnDefinitions.Add(new ColumnDefinition());
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    grid.ColumnDefinitions.Add(new ColumnDefinition());
                    AddFullscreenCell(grid, 0, 0, 1, 1, 0);
                    AddGridSplitter(grid, 0, 1, 1, true);
                    AddFullscreenCell(grid, 0, 2, 1, 1, 1);
                    break;
                case "3":
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    grid.ColumnDefinitions.Add(new ColumnDefinition());
                    grid.RowDefinitions.Add(new RowDefinition());
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition());
                    AddFullscreenCell(grid, 0, 0, 3, 1, 0);
                    AddGridSplitter(grid, 0, 1, 3, true);
                    AddFullscreenCell(grid, 0, 2, 1, 1, 1);
                    AddHSplitter(grid, 1, 2);
                    AddFullscreenCell(grid, 2, 2, 1, 1, 2);
                    break;
                case "4":
                    grid.ColumnDefinitions.Add(new ColumnDefinition());
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    grid.ColumnDefinitions.Add(new ColumnDefinition());
                    grid.RowDefinitions.Add(new RowDefinition());
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition());
                    AddFullscreenCell(grid, 0, 0, 1, 1, 0);
                    AddFullscreenCell(grid, 0, 2, 1, 1, 1);
                    AddFullscreenCell(grid, 2, 0, 1, 1, 2);
                    AddFullscreenCell(grid, 2, 2, 1, 1, 3);
                    AddGridSplitter(grid, 0, 1, 3, true);
                    AddHSplitter(grid, 1, 0, 3);
                    break;
                case "pip":
                    AddFullscreenCell(grid, 0, 0, 1, 1, 0);
                    if (_panelAssignments.TryGetValue(1, out var pipOpt))
                    {
                        var pipContent = BuildPanelContent(pipOpt);
                        var pipBorder = new Border
                        {
                            Width = 400, Height = 260,
                            Background = new SolidColorBrush(Color.FromRgb(30, 46, 62)),
                            CornerRadius = new CornerRadius(8),
                            VerticalAlignment = VerticalAlignment.Bottom,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Margin = new Thickness(0, 0, 24, 24),
                            BorderBrush = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                            BorderThickness = new Thickness(2),
                            Child = pipContent
                        };
                        Panel.SetZIndex(pipBorder, 50);
                        grid.Children.Add(pipBorder);
                    }
                    break;
            }
        }

        private void AddFullscreenCell(Grid grid, int row, int col, int rowSpan, int colSpan, int panelIndex)
        {
            UIElement content;
            if (_panelAssignments.TryGetValue(panelIndex, out var opt))
            {
                content = BuildPanelContent(opt);
            }
            else
            {
                // Empty panel
                var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                sp.Children.Add(new TextBlock { Text = $"Vùng {panelIndex + 1}", FontSize = 24, Foreground = new SolidColorBrush(Color.FromRgb(69, 90, 100)), HorizontalAlignment = HorizontalAlignment.Center });
                sp.Children.Add(new TextBlock { Text = "Chưa gán nội dung", FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(55, 71, 79)), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) });
                content = sp;
            }

            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(18, 32, 50)),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(2),
                Child = content
            };

            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            Grid.SetRowSpan(border, Math.Max(1, rowSpan));
            Grid.SetColumnSpan(border, Math.Max(1, colSpan));
            grid.Children.Add(border);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Content options for split screen panels
    // ═══════════════════════════════════════════════════════════

    public record ContentOption(string Icon, string Name, string Description, string AccentColor);
}
 