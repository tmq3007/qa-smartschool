using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class LessonListPage : Page
    {
        private List<Lesson> _allLessons = new();
        private string _currentFilter  = "All";
        private string _currentSort    = "UpdatedAt";
        private bool   _sortDesc       = true;
        private string _viewMode       = "list"; // list | grid
        private string _activeClassName = "";
        private string _teacherScope   = "mine"; // "mine" | "all"
        private string _loggedInTeacher = "";

        public LessonListPage()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                LoadLessons();

                // Auto-refresh khi GV chuyển lớp — N7 FIX: Dùng named handler để Unsubscribe tránh memory leak
                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    app.ClassRoster.ActiveRosterChanged -= OnActiveRosterChanged;
                    app.ClassRoster.ActiveRosterChanged += OnActiveRosterChanged;
                }
                catch { }
            };

            Unloaded += (_, _) =>
            {
                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    app.ClassRoster.ActiveRosterChanged -= OnActiveRosterChanged;
                }
                catch { }
            };
        }

        private void OnActiveRosterChanged(object? sender, Data.ClassRoster? e)
        {
            Dispatcher.Invoke(() => LoadLessons());
        }

        // ═════════════════════════════════════════════════════
        //  LOAD & FILTER
        // ═════════════════════════════════════════════════════

        private void LoadLessons()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var db = app.Database;
                var roster = app.ClassRoster.ActiveRoster;
                _activeClassName = roster?.ClassName ?? "";

                // Lấy tên GV đăng nhập từ TeacherProfile
                if (string.IsNullOrEmpty(_loggedInTeacher))
                {
                    try
                    {
                        var profile = db.TeacherProfiles.FirstOrDefault();
                        _loggedInTeacher = profile?.FullName ?? "";
                    }
                    catch { }
                }

                IQueryable<Lesson> query = db.Lessons;

                // Filter theo lớp active
                if (!string.IsNullOrEmpty(_activeClassName))
                    query = query.Where(l => l.ClassName == _activeClassName);

                // Filter theo GV đăng nhập (mặc định)
                if (_teacherScope == "mine" && !string.IsNullOrEmpty(_loggedInTeacher))
                    query = query.Where(l => l.TeacherName == _loggedInTeacher);

                _allLessons = query.OrderByDescending(l => l.UpdatedAt).ToList();

                UpdateSummary();
                ApplyFilter();
                Log.Information("LessonListPage loaded: {Count} lessons for class {Class}, scope={Scope}, teacher={Teacher}",
                    _allLessons.Count, _activeClassName, _teacherScope, _loggedInTeacher);
            }
            catch (Exception ex)
            {
                Log.Warning("LoadLessons error: {Error}", ex.Message);
            }
        }

        private void ApplyFilter()
        {
            // Guard: skip if XAML elements haven't initialized yet
            if (txtSearch == null || lessonGrid == null || txtResultCount == null) return;

            var filtered = _allLessons.AsEnumerable();

            // Status / category filter
            filtered = _currentFilter switch
            {
                "Favorite"        => filtered.Where(l => l.IsFavorite),
                "Draft"           => filtered.Where(l => l.Status == "Draft"),
                "PendingApproval" => filtered.Where(l => l.Status == "PendingApproval"),
                "Approved"        => filtered.Where(l => l.Status == "Approved"),
                "Taught"          => filtered.Where(l => l.Status == "Taught"),
                "All"             => filtered,
                _                 => filtered.Where(l => l.Subject == _currentFilter)
            };

            // Search
            var q = txtSearch.Text?.Trim() ?? "";
            if (!string.IsNullOrWhiteSpace(q) && q != "🔍 Tìm kiếm bài giảng...")
                filtered = filtered.Where(l =>
                    l.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    l.Subject.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    l.Grade.Contains(q, StringComparison.OrdinalIgnoreCase));

            // Sort
            filtered = _currentSort switch
            {
                "Title"     => _sortDesc ? filtered.OrderByDescending(l => l.Title)     : filtered.OrderBy(l => l.Title),
                "Subject"   => _sortDesc ? filtered.OrderByDescending(l => l.Subject)   : filtered.OrderBy(l => l.Subject),
                "UseCount"  => _sortDesc ? filtered.OrderByDescending(l => l.UseCount)  : filtered.OrderBy(l => l.UseCount),
                _           => _sortDesc ? filtered.OrderByDescending(l => l.UpdatedAt) : filtered.OrderBy(l => l.UpdatedAt)
            };

            var result = filtered.Select(l => new LessonViewModel
            {
                Id          = l.Id,
                Title       = l.Title,
                Subject     = l.Subject,
                Grade       = l.Grade,
                Description = l.Description,
                Status      = l.Status,
                IsFavorite  = l.IsFavorite,
                UseCount    = l.UseCount,
                UpdatedAt   = l.UpdatedAt,
                LessonType  = l.LessonType,
                DurationMin = l.DurationMinutes,
                // Timetable
                Period         = l.Period,
                DayOfWeekStr   = l.DayOfWeek,
                WeekNumber     = l.WeekNumber,
                ClassName      = l.ClassName,
                TeacherName    = l.TeacherName,
                SubstituteTeacher = l.SubstituteTeacher,
                // Derived display properties
                StatusBg    = GetStatusBg(l.Status),
                StatusFg    = GetStatusFg(l.Status),
                StatusLabel = GetStatusLabel(l.Status),
                SubjectIcon = GetSubjectIcon(l.Subject),
                AccentBrush = GetAccentBrush(l.Subject),
                TypeIcon    = l.LessonType switch { "STEAM" => "⚗️", "Workshop" => "🔨", "Exam" => "📝", _ => "📖" },
                FavIcon     = l.IsFavorite ? "⭐" : "☆",
                UsedLabel   = l.UseCount > 0 ? $"Đã dạy {l.UseCount}x" : "Chưa dạy",
                CardBg      = l.Status == "Taught" ? "#FAFFFE" : l.IsFavorite ? "#FFFDF5" : "White"
            }).ToList();

            // Render both views
            lessonGrid.ItemsSource = result;
            RenderListView(result);
            var scopeLabel = _teacherScope == "mine" && !string.IsNullOrEmpty(_loggedInTeacher)
                ? $"👤 {_loggedInTeacher}" : "👥 Tất cả GV";
            txtResultCount.Text = $"{result.Count} bài giảng  •  {_allLessons.Count} tổng  •  {scopeLabel}";
        }

        private void UpdateSummary()
        {
            txtTotalCount.Text    = _allLessons.Count.ToString();
            txtApprovedCount.Text = _allLessons.Count(l => l.Status is "Approved" or "Taught").ToString();
            txtDraftCount.Text    = _allLessons.Count(l => l.Status == "Draft").ToString();
            txtPendingCount.Text  = _allLessons.Count(l => l.Status == "PendingApproval").ToString();
        }

        // ═════════════════════════════════════════════════════
        //  FILTER & SORT EVENTS
        // ═════════════════════════════════════════════════════

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                _currentFilter = tag;
                // Highlight active button
                HighlightFilterBtn(btn);
                ApplyFilter();
            }
        }

        private void HighlightFilterBtn(Button active)
        {
            foreach (var elem in filterBar.Children)
            {
                if (elem is Button b)
                {
                    bool isActive = b == active;
                    b.Background = isActive
                        ? new SolidColorBrush(Color.FromRgb(25, 118, 210))
                        : new SolidColorBrush(Color.FromRgb(245, 245, 245));
                    b.Foreground = isActive ? Brushes.White : new SolidColorBrush(Color.FromRgb(66, 66, 66));
                }
            }
        }

        private void Sort_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSort?.SelectedItem is ComboBoxItem item)
            {
                _currentSort = item.Tag?.ToString() ?? "UpdatedAt";
                ApplyFilter();
            }
        }

        private void SortDir_Click(object sender, RoutedEventArgs e)
        {
            _sortDesc = !_sortDesc;
            btnSortDir.Content = _sortDesc ? "↓ Giảm" : "↑ Tăng";
            ApplyFilter();
        }

        private void Search_Changed(object sender, TextChangedEventArgs e) => ApplyFilter();
        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadLessons();

        // ═════════════════════════════════════════════════════
        //  TEACHER SCOPE TOGGLE  (Của tôi / Tất cả GV)
        // ═════════════════════════════════════════════════════

        private void ScopeMyLessons_Click(object sender, RoutedEventArgs e)
        {
            if (_teacherScope == "mine") return;
            _teacherScope = "mine";
            UpdateScopeToggleUI();
            LoadLessons();
        }

        private void ScopeAllTeachers_Click(object sender, RoutedEventArgs e)
        {
            if (_teacherScope == "all") return;
            _teacherScope = "all";
            UpdateScopeToggleUI();
            LoadLessons();
        }

        private void UpdateScopeToggleUI()
        {
            if (btnScopeMyLessons == null || btnScopeAll == null) return;

            var activeBg = new SolidColorBrush(Color.FromRgb(21, 101, 192)); // #1565C0
            var inactiveBg = Brushes.Transparent;
            var activeFg = Brushes.White;
            var inactiveFg = new SolidColorBrush(Color.FromRgb(73, 80, 87)); // #495057

            bool isMine = _teacherScope == "mine";
            btnScopeMyLessons.Background = isMine ? activeBg : inactiveBg;
            btnScopeMyLessons.Foreground = isMine ? activeFg : inactiveFg;
            btnScopeMyLessons.FontWeight = isMine ? FontWeights.SemiBold : FontWeights.Normal;
            btnScopeAll.Background = isMine ? inactiveBg : activeBg;
            btnScopeAll.Foreground = isMine ? inactiveFg : activeFg;
            btnScopeAll.FontWeight = isMine ? FontWeights.Normal : FontWeights.SemiBold;
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == "🔍 Tìm kiếm bài giảng...")
            {
                txtSearch.Text = "";
                txtSearch.Foreground = new SolidColorBrush(Color.FromRgb(33, 37, 41));
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = "🔍 Tìm kiếm bài giảng...";
                txtSearch.Foreground = new SolidColorBrush(Color.FromRgb(173, 181, 189));
            }
        }

        // ═════════════════════════════════════════════════════
        //  VIEW MODE TOGGLE
        // ═════════════════════════════════════════════════════

        private void ViewList_Click(object sender, RoutedEventArgs e)
        {
            _viewMode = "list";
            listViewPanel.Visibility = Visibility.Visible;
            gridViewPanel.Visibility = Visibility.Collapsed;
            btnViewList.Background = new SolidColorBrush(Color.FromRgb(25, 118, 210));
            btnViewList.Foreground = Brushes.White;
            btnViewGrid.Background = Brushes.Transparent;
            btnViewGrid.Foreground = new SolidColorBrush(Color.FromRgb(73, 80, 87));
        }

        private void ViewGrid_Click(object sender, RoutedEventArgs e)
        {
            _viewMode = "grid";
            listViewPanel.Visibility = Visibility.Collapsed;
            gridViewPanel.Visibility = Visibility.Visible;
            btnViewGrid.Background = new SolidColorBrush(Color.FromRgb(25, 118, 210));
            btnViewGrid.Foreground = Brushes.White;
            btnViewList.Background = Brushes.Transparent;
            btnViewList.Foreground = new SolidColorBrush(Color.FromRgb(73, 80, 87));
        }

        // ═════════════════════════════════════════════════════
        //  LIST VIEW RENDERER
        // ═════════════════════════════════════════════════════

        private void RenderListView(List<LessonViewModel> items)
        {
            listViewBody.Children.Clear();

            if (items.Count == 0)
            {
                var empty = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 60, 0, 0) };
                empty.Children.Add(new TextBlock { Text = "📚", FontSize = 48, HorizontalAlignment = HorizontalAlignment.Center });
                empty.Children.Add(new TextBlock
                {
                    Text = "Chưa có bài giảng nào", FontSize = 15,
                    Foreground = new SolidColorBrush(Color.FromRgb(134, 142, 150)),
                    HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0)
                });
                listViewBody.Children.Add(empty);
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                var vm = items[i];
                bool isEven = i % 2 == 0;

                var rowBorder = new Border
                {
                    Background = isEven ? Brushes.White : new SolidColorBrush(Color.FromRgb(248, 249, 250)),
                    Padding = new Thickness(16, 9, 16, 9),
                    Cursor = Cursors.Hand,
                    DataContext = vm
                };

                rowBorder.MouseEnter += (_, _) => rowBorder.Background = new SolidColorBrush(Color.FromRgb(240, 245, 255));
                rowBorder.MouseLeave += (_, _) => rowBorder.Background = isEven ? Brushes.White : new SolidColorBrush(Color.FromRgb(248, 249, 250));
                rowBorder.MouseLeftButtonDown += LessonCard_Click;

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(95) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(65) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });

                // #
                var numTb = new TextBlock { Text = (i + 1).ToString(), FontSize = 10.5, Foreground = new SolidColorBrush(Color.FromRgb(173, 181, 189)), VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(numTb, 0); grid.Children.Add(numTb);

                // Fav
                var favBtn = new Button
                {
                    Content = vm.FavIcon, FontSize = 13, Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0), Cursor = Cursors.Hand, Padding = new Thickness(2),
                    VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center,
                    DataContext = vm
                };
                favBtn.Click += ToggleFavorite_Click;
                Grid.SetColumn(favBtn, 1); grid.Children.Add(favBtn);

                // Title
                var titleSp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
                titleRow.Children.Add(new TextBlock { Text = vm.SubjectIcon, FontSize = 13, Margin = new Thickness(0, 0, 5, 0) });
                titleRow.Children.Add(new TextBlock
                {
                    Text = vm.Title, FontSize = 12, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 37, 41)),
                    TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 300
                });
                titleSp.Children.Add(titleRow);
                if (!string.IsNullOrWhiteSpace(vm.Description))
                {
                    titleSp.Children.Add(new TextBlock
                    {
                        Text = vm.Description, FontSize = 9.5,
                        Foreground = new SolidColorBrush(Color.FromRgb(173, 181, 189)),
                        TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 300,
                        Margin = new Thickness(18, 1, 0, 0)
                    });
                }
                Grid.SetColumn(titleSp, 2); grid.Children.Add(titleSp);

                // Subject
                var subTb = new TextBlock
                {
                    Text = vm.Subject, FontSize = 10.5,
                    Foreground = new SolidColorBrush(Color.FromRgb(73, 80, 87)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(subTb, 3); grid.Children.Add(subTb);

                // Grade
                var gradeTb = new TextBlock
                {
                    Text = vm.Grade, FontSize = 10.5,
                    Foreground = new SolidColorBrush(Color.FromRgb(134, 142, 150)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(gradeTb, 4); grid.Children.Add(gradeTb);

                // Timetable
                var ttLabel = vm.Period > 0 ? $"{vm.DayOfWeekStr} · T{vm.Period}" : "—";
                var ttTb = new TextBlock
                {
                    Text = ttLabel, FontSize = 10,
                    Foreground = vm.Period > 0 ? new SolidColorBrush(Color.FromRgb(21, 101, 192)) : new SolidColorBrush(Color.FromRgb(206, 212, 218)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(ttTb, 5); grid.Children.Add(ttTb);

                // Status badge
                var statusBorder = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(vm.StatusBg)!),
                    CornerRadius = new CornerRadius(6), Padding = new Thickness(6, 3, 6, 3),
                    HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center
                };
                statusBorder.Child = new TextBlock
                {
                    Text = vm.StatusLabel, FontSize = 9.5, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(vm.StatusFg)!)
                };
                Grid.SetColumn(statusBorder, 6); grid.Children.Add(statusBorder);

                // Use count
                var useTb = new TextBlock
                {
                    Text = vm.UseCount > 0 ? $"{vm.UseCount}x" : "—",
                    FontSize = 10.5, FontWeight = vm.UseCount > 0 ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = vm.UseCount > 0 ? new SolidColorBrush(Color.FromRgb(25, 118, 210)) : new SolidColorBrush(Color.FromRgb(206, 212, 218)),
                    VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(useTb, 7); grid.Children.Add(useTb);

                // Updated
                var updTb = new TextBlock
                {
                    Text = vm.UpdatedLabel, FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(173, 181, 189)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(updTb, 8); grid.Children.Add(updTb);

                // Action buttons
                var actionSp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

                var btnTeach = CreateActionBtn("🚀", "#E3F2FD", "Dạy");
                btnTeach.DataContext = vm;
                btnTeach.Click += StartLesson_Click;
                actionSp.Children.Add(btnTeach);

                var btnEdit = CreateActionBtn("✏️", "#F1F3F5", "Sửa");
                btnEdit.DataContext = vm;
                btnEdit.Click += EditLesson_Click;
                actionSp.Children.Add(btnEdit);

                var btnCopy = CreateActionBtn("📋", "#F1F3F5", "Sao chép");
                btnCopy.DataContext = vm;
                btnCopy.Click += DuplicateLesson_Click;
                actionSp.Children.Add(btnCopy);

                var btnDel = CreateActionBtn("🗑️", "#FFF5F5", "Xóa");
                btnDel.DataContext = vm;
                btnDel.Click += DeleteLesson_Click;
                actionSp.Children.Add(btnDel);

                Grid.SetColumn(actionSp, 9); grid.Children.Add(actionSp);

                rowBorder.Child = grid;
                listViewBody.Children.Add(rowBorder);

                // Separator line
                listViewBody.Children.Add(new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.FromRgb(241, 243, 245))
                });
            }
        }

        private Button CreateActionBtn(string icon, string bg, string tooltip)
        {
            return new Button
            {
                Content = icon, FontSize = 11,
                Padding = new Thickness(6, 4, 6, 4),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg)!),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand, ToolTip = tooltip,
                Margin = new Thickness(0, 0, 3, 0)
            };
        }

        // ═════════════════════════════════════════════════════
        //  CARD ACTIONS
        // ═════════════════════════════════════════════════════

        private void NewLesson_Click(object sender, RoutedEventArgs e)
        {
            ShowLessonCreateDialog();
        }

        /// <summary>Dialog tạo bài giảng mới — tích hợp thời khóa biểu</summary>
        private void ShowLessonCreateDialog()
        {
            var wnd = new Window
            {
                Title = "➕ Tạo bài giảng mới", Width = 560, Height = 620,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this), ResizeMode = ResizeMode.NoResize,
                Background = Brushes.White
            };
            var sp = new StackPanel { Margin = new Thickness(20, 14, 20, 14) };

            sp.Children.Add(new TextBlock { Text = "📚 Tạo bài giảng mới", FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 14) });

            // ── Title ──
            sp.Children.Add(MkLabel("📝 Tiêu đề bài giảng: *"));
            var tbTitle = new TextBox { FontSize = 13, Padding = new Thickness(10, 7, 10, 7), Margin = new Thickness(0, 0, 0, 8) };
            sp.Children.Add(tbTitle);

            // ── Row: Subject + Grade + Class ──
            var r1 = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            r1.ColumnDefinitions.Add(new ColumnDefinition());
            r1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            r1.ColumnDefinitions.Add(new ColumnDefinition());
            r1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            r1.ColumnDefinitions.Add(new ColumnDefinition());

            var subPanel = new StackPanel(); subPanel.Children.Add(MkLabel("📐 Môn học:"));
            var cmbSub = new ComboBox { FontSize = 12, Padding = new Thickness(6, 4, 6, 4), SelectedIndex = 0 };
            foreach (var s in new[] { "Toán", "Vật lý", "Hóa học", "Sinh học", "Ngữ văn", "Tiếng Anh", "Lịch sử", "Địa lý", "GDCD", "Tin học", "Công nghệ", "GDTC" })
                cmbSub.Items.Add(s);
            subPanel.Children.Add(cmbSub);
            Grid.SetColumn(subPanel, 0); r1.Children.Add(subPanel);

            var gradePanel = new StackPanel(); gradePanel.Children.Add(MkLabel("🎓 Khối:"));
            var cmbGrade = new ComboBox { FontSize = 12, Padding = new Thickness(6, 4, 6, 4), SelectedIndex = 0 };
            foreach (var g in new[] { "Lớp 10", "Lớp 11", "Lớp 12", "Lớp 6", "Lớp 7", "Lớp 8", "Lớp 9" })
                cmbGrade.Items.Add(g);
            gradePanel.Children.Add(cmbGrade);
            Grid.SetColumn(gradePanel, 2); r1.Children.Add(gradePanel);

            var classPanel = new StackPanel(); classPanel.Children.Add(MkLabel("🏫 Lớp:"));
            var tbClass = new TextBox { Text = _activeClassName, FontSize = 12, Padding = new Thickness(6, 4, 6, 4) };
            classPanel.Children.Add(tbClass);
            Grid.SetColumn(classPanel, 4); r1.Children.Add(classPanel);
            sp.Children.Add(r1);

            // ── Row: Day + Period + Duration ──
            sp.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromRgb(230, 230, 230)), Margin = new Thickness(0, 2, 0, 6) });
            sp.Children.Add(new TextBlock { Text = "📅 THỜI KHÓA BIỂU", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210)), Margin = new Thickness(0, 0, 0, 4) });

            var r2 = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            r2.ColumnDefinitions.Add(new ColumnDefinition());
            r2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            r2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            r2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            r2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

            var dayPanel = new StackPanel(); dayPanel.Children.Add(MkLabel("📅 Thứ:"));
            var cmbDay = new ComboBox { FontSize = 12, Padding = new Thickness(6, 4, 6, 4), SelectedIndex = 0 };
            foreach (var d in new[] { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7" })
                cmbDay.Items.Add(d);
            dayPanel.Children.Add(cmbDay);
            Grid.SetColumn(dayPanel, 0); r2.Children.Add(dayPanel);

            var periodPanel = new StackPanel(); periodPanel.Children.Add(MkLabel("⏰ Tiết:"));
            var cmbPeriod = new ComboBox { FontSize = 12, Padding = new Thickness(6, 4, 6, 4), SelectedIndex = 0 };
            foreach (var p in new[] { "Tiết 1", "Tiết 2", "Tiết 3", "Tiết 4", "Tiết 5", "Tiết 6", "Tiết 7", "Tiết 8", "Tiết 9", "Tiết 10" })
                cmbPeriod.Items.Add(p);
            periodPanel.Children.Add(cmbPeriod);
            Grid.SetColumn(periodPanel, 2); r2.Children.Add(periodPanel);

            var durPanel = new StackPanel(); durPanel.Children.Add(MkLabel("⏱ Phút:"));
            var cmbDur = new ComboBox { FontSize = 12, Padding = new Thickness(6, 4, 6, 4), SelectedIndex = 0 };
            cmbDur.Items.Add("45"); cmbDur.Items.Add("90"); cmbDur.Items.Add("35");
            durPanel.Children.Add(cmbDur);
            Grid.SetColumn(durPanel, 4); r2.Children.Add(durPanel);
            sp.Children.Add(r2);

            // ── Row: Week + Semester ──
            var r2b = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            r2b.ColumnDefinitions.Add(new ColumnDefinition());
            r2b.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            r2b.ColumnDefinitions.Add(new ColumnDefinition());

            var weekPanel = new StackPanel(); weekPanel.Children.Add(MkLabel("📆 Tuần học:"));
            var tbWeek = new TextBox { Text = "1", FontSize = 12, Padding = new Thickness(6, 4, 6, 4) };
            weekPanel.Children.Add(tbWeek);
            Grid.SetColumn(weekPanel, 0); r2b.Children.Add(weekPanel);

            var semPanel = new StackPanel(); semPanel.Children.Add(MkLabel("🗓️ Học kỳ:"));
            var cmbSem = new ComboBox { FontSize = 12, Padding = new Thickness(6, 4, 6, 4), SelectedIndex = 0 };
            cmbSem.Items.Add("HK1"); cmbSem.Items.Add("HK2");
            semPanel.Children.Add(cmbSem);
            Grid.SetColumn(semPanel, 2); r2b.Children.Add(semPanel);
            sp.Children.Add(r2b);

            // ── Row: Teacher + Substitute ──
            sp.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromRgb(230, 230, 230)), Margin = new Thickness(0, 2, 0, 6) });
            sp.Children.Add(new TextBlock { Text = "👨‍🏫 GIÁO VIÊN GIẢNG DẠY", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)), Margin = new Thickness(0, 0, 0, 4) });

            var r3 = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            r3.ColumnDefinitions.Add(new ColumnDefinition());
            r3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            r3.ColumnDefinitions.Add(new ColumnDefinition());

            var teachPanel = new StackPanel(); teachPanel.Children.Add(MkLabel("👨‍🏫 GV chính:"));
            var tbTeacher = new TextBox { Text = _loggedInTeacher, FontSize = 12, Padding = new Thickness(6, 4, 6, 4) };
            teachPanel.Children.Add(tbTeacher);
            Grid.SetColumn(teachPanel, 0); r3.Children.Add(teachPanel);

            var subTeachPanel = new StackPanel(); subTeachPanel.Children.Add(MkLabel("🔄 GV dạy thay:"));
            var tbSubTeacher = new TextBox { FontSize = 12, Padding = new Thickness(6, 4, 6, 4) };
            subTeachPanel.Children.Add(tbSubTeacher);
            Grid.SetColumn(subTeachPanel, 2); r3.Children.Add(subTeachPanel);
            sp.Children.Add(r3);

            // Reason
            sp.Children.Add(MkLabel("📝 Lý do dạy thay (nếu có):"));
            var tbReason = new TextBox { FontSize = 11, Padding = new Thickness(6, 4, 6, 4), Margin = new Thickness(0, 0, 0, 8) };
            sp.Children.Add(tbReason);

            // ── Description ──
            sp.Children.Add(MkLabel("📋 Mô tả:"));
            var tbDesc = new TextBox { FontSize = 12, Padding = new Thickness(8, 6, 8, 6), AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 40, Margin = new Thickness(0, 0, 0, 12) };
            sp.Children.Add(tbDesc);

            // ── Save button ──
            var btnSave = new Button
            {
                Content = "✅ Tạo bài giảng", FontSize = 13, Padding = new Thickness(0, 10, 0, 10),
                Background = new SolidColorBrush(Color.FromRgb(0, 105, 92)), Foreground = Brushes.White,
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand
            };
            btnSave.Click += (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(tbTitle.Text)) { MessageBox.Show("Nhập tiêu đề!"); return; }
                try
                {
                    int.TryParse(tbWeek.Text, out int week); if (week <= 0) week = 1;
                    int.TryParse(cmbDur.SelectedItem?.ToString(), out int dur); if (dur <= 0) dur = 45;
                    var db = ((QASmartTouch.App)Application.Current).Database;
                    db.Lessons.Add(new Lesson
                    {
                        Title = tbTitle.Text.Trim(),
                        Subject = cmbSub.SelectedItem?.ToString() ?? "Toán",
                        Grade = cmbGrade.SelectedItem?.ToString() ?? "Lớp 10",
                        ClassName = tbClass.Text.Trim(),
                        DayOfWeek = cmbDay.SelectedItem?.ToString() ?? "",
                        Period = cmbPeriod.SelectedIndex + 1,
                        DurationMinutes = dur,
                        WeekNumber = week,
                        Semester = cmbSem.SelectedItem?.ToString() ?? "HK1",
                        TeacherName = tbTeacher.Text.Trim(),
                        SubstituteTeacher = tbSubTeacher.Text.Trim(),
                        SubstituteReason = tbReason.Text.Trim(),
                        Description = tbDesc.Text.Trim(),
                        Status = "Draft",
                        CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now
                    });
                    db.SaveChanges();
                    wnd.Close();
                    LoadLessons();
                    MessageBox.Show("✅ Đã tạo bài giảng!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
            };
            sp.Children.Add(btnSave);

            wnd.Content = new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            wnd.ShowDialog();
        }

        private void EditLesson_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is LessonViewModel vm)
            {
                if (Window.GetWindow(this) is ClassroomShell shell)
                    shell.NavigateToEditor(vm.Id);
            }
            e.Handled = true;
        }

        private void LessonCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is LessonViewModel vm)
            {
                Log.Information("Open lesson: {Title} (ID={Id})", vm.Title, vm.Id);
                if (Window.GetWindow(this) is ClassroomShell shell)
                    shell.NavigateToEditor(vm.Id);
            }
        }

        private void StartLesson_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is LessonViewModel vm)
            {
                if (Window.GetWindow(this) is ClassroomShell shell)
                    shell.NavigateToRunner(vm.Id);
            }
            e.Handled = true;
        }

        private void ToggleFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is LessonViewModel vm)
            {
                try
                {
                    var db     = ((QASmartTouch.App)Application.Current).Database;
                    var lesson = db.Lessons.Find(vm.Id);
                    if (lesson != null)
                    {
                        lesson.IsFavorite = !lesson.IsFavorite;
                        db.SaveChanges();
                        LoadLessons();
                    }
                }
                catch (Exception ex) { Log.Warning("ToggleFav: {Err}", ex.Message); }
            }
            e.Handled = true;
        }

        private void DeleteLesson_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is LessonViewModel vm)
            {
                var r = MessageBox.Show($"Xoá bài giảng:\n\"{vm.Title}\"?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (r != MessageBoxResult.Yes) return;
                try
                {
                    var db = ((QASmartTouch.App)Application.Current).Database;
                    var lesson = db.Lessons.Find(vm.Id);
                    if (lesson != null) { db.Lessons.Remove(lesson); db.SaveChanges(); }
                    LoadLessons();
                }
                catch (Exception ex) { Log.Warning("DeleteLesson: {Err}", ex.Message); }
            }
            e.Handled = true;
        }

        private void DuplicateLesson_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is LessonViewModel vm)
            {
                try
                {
                    var db     = ((QASmartTouch.App)Application.Current).Database;
                    var lesson = db.Lessons.Find(vm.Id);
                    if (lesson != null)
                    {
                        var copy = new Lesson
                        {
                            Title          = $"{lesson.Title} (Bản sao)",
                            Subject        = lesson.Subject,
                            Grade          = lesson.Grade,
                            Description    = lesson.Description,
                            LessonType     = lesson.LessonType,
                            DurationMinutes= lesson.DurationMinutes,
                            Status         = "Draft",
                            Tags           = lesson.Tags,
                            ClassName      = lesson.ClassName,
                            DayOfWeek      = lesson.DayOfWeek,
                            Period         = lesson.Period,
                            WeekNumber     = lesson.WeekNumber,
                            Semester       = lesson.Semester,
                            TeacherName    = lesson.TeacherName,
                            CreatedAt      = DateTime.Now,
                            UpdatedAt      = DateTime.Now
                        };
                        db.Lessons.Add(copy);
                        db.SaveChanges();
                        LoadLessons();
                        MessageBox.Show($"✅ Đã sao chép:\n\"{copy.Title}\"",
                            "Sao chép thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex) { Log.Warning("Duplicate: {Err}", ex.Message); }
            }
            e.Handled = true;
        }

        private void QuickApprove_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is LessonViewModel vm)
            {
                try
                {
                    var db     = ((QASmartTouch.App)Application.Current).Database;
                    var lesson = db.Lessons.Find(vm.Id);
                    if (lesson != null && lesson.Status == "PendingApproval")
                    {
                        lesson.Status    = "Approved";
                        lesson.UpdatedAt = DateTime.Now;
                        db.SaveChanges();
                        LoadLessons();
                    }
                }
                catch (Exception ex) { Log.Warning("QuickApprove: {Err}", ex.Message); }
            }
            e.Handled = true;
        }

        // ═════════════════════════════════════════════════════
        //  EXPORT LIST
        // ═════════════════════════════════════════════════════

        private void ExportList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("STT,Tiêu đề,Môn học,Lớp,Thứ,Tiết,Tuần,GV chính,GV dạy thay,Trạng thái,Số lần dạy,Cập nhật");
                int i = 1;
                foreach (var l in _allLessons.OrderBy(x => x.Subject).ThenBy(x => x.Title))
                    sb.AppendLine($"{i++},\"{l.Title}\",{l.Subject},{l.ClassName},{l.DayOfWeek},Tiết {l.Period},Tuần {l.WeekNumber},{l.TeacherName},{l.SubstituteTeacher},{l.Status},{l.UseCount},{l.UpdatedAt:dd/MM/yyyy}");

                string folder = QASmartClass.Services.AppPaths.DocumentsDir;
                System.IO.Directory.CreateDirectory(folder);
                string path = System.IO.Path.Combine(folder, $"LessonList_{DateTime.Now:yyyyMMdd_HHmm}.csv");
                System.IO.File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
                System.Diagnostics.Process.Start("explorer.exe", folder);
                MessageBox.Show($"✅ Đã xuất {_allLessons.Count} bài giảng!\n{path}",
                    "Xuất danh sách", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═════════════════════════════════════════════════════
        //  TIMETABLE VIEW
        // ═════════════════════════════════════════════════════

        private void Timetable_Click(object sender, RoutedEventArgs e)
        {
            var wnd = new Window
            {
                Title = "📅 Thời khóa biểu", Width = 960, Height = 640,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this), Background = Brushes.White
            };
            
            var mainSp = new StackPanel { Margin = new Thickness(16) };
            
            var headerDock = new DockPanel { Margin = new Thickness(0, 0, 0, 16) };
            var titleTb = new TextBlock { Text = "📅 Thời khóa biểu", FontSize = 18, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center };
            DockPanel.SetDock(titleTb, Dock.Left);
            headerDock.Children.Add(titleTb);

            var typeCb = new ComboBox { Width = 280, FontSize = 13, Padding = new Thickness(8, 4, 8, 4), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            
            string classLabel = !string.IsNullOrEmpty(_activeClassName) ? $"Lớp {_activeClassName}" : "Lớp hiện tại";
            string teacherLabel = !string.IsNullOrEmpty(_loggedInTeacher) ? $"Giáo viên {_loggedInTeacher}" : "Giáo viên hiện tại";
            
            typeCb.Items.Add(new ComboBoxItem { Content = $"🏫 Thời khóa biểu {classLabel}", Tag = "Class" });
            typeCb.Items.Add(new ComboBoxItem { Content = $"👨‍🏫 Thời khóa biểu {teacherLabel}", Tag = "Teacher" });
            typeCb.SelectedIndex = 0;
            DockPanel.SetDock(typeCb, Dock.Right);
            headerDock.Children.Add(typeCb);
            
            mainSp.Children.Add(headerDock);
            
            var gridContainer = new ContentControl();
            mainSp.Children.Add(gridContainer);
            
            Action renderGrid = () => {
                string mode = (typeCb.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Class";
                
                var db = ((QASmartTouch.App)Application.Current).Database;
                List<Lesson> lessons = new List<Lesson>();
                
                if (mode == "Class") {
                    if (!string.IsNullOrEmpty(_activeClassName))
                        lessons = db.Lessons.Where(l => l.ClassName == _activeClassName).ToList();
                    else
                        lessons = _allLessons; // fallback
                } else {
                    if (!string.IsNullOrEmpty(_loggedInTeacher))
                        lessons = db.Lessons.Where(l => l.TeacherName == _loggedInTeacher).ToList();
                    else
                        lessons = _allLessons; // fallback
                }

                var days = new[] { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7" };
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
                for (int d = 0; d < 6; d++)
                    grid.ColumnDefinitions.Add(new ColumnDefinition());
                
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                for (int d = 0; d < 6; d++)
                {
                    var hdr = new Border { Background = new SolidColorBrush(Color.FromRgb(25, 118, 210)), Padding = new Thickness(4, 8, 4, 8), Margin = new Thickness(1), CornerRadius = new CornerRadius(2) };
                    hdr.Child = new TextBlock { Text = days[d], FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center };
                    Grid.SetRow(hdr, 0); Grid.SetColumn(hdr, d + 1); grid.Children.Add(hdr);
                }

                for (int p = 1; p <= 10; p++)
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    
                    var pLabel = new Border
                    {
                        Background = p <= 5 ? new SolidColorBrush(Color.FromRgb(232, 245, 233)) : new SolidColorBrush(Color.FromRgb(255, 248, 225)),
                        Padding = new Thickness(4, 8, 4, 8), Margin = new Thickness(1), CornerRadius = new CornerRadius(2)
                    };
                    pLabel.Child = new TextBlock { Text = $"Tiết {p}", FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center, FontWeight = FontWeights.SemiBold };
                    Grid.SetRow(pLabel, p); Grid.SetColumn(pLabel, 0); grid.Children.Add(pLabel);

                    for (int d = 0; d < 6; d++)
                    {
                        string dayStr = days[d];
                        int period = p;
                        var lessonListForCell = lessons.Where(l => l.DayOfWeek == dayStr && l.Period == period).ToList();
                        
                        var cell = new Border
                        {
                            Background = lessonListForCell.Any() ? new SolidColorBrush(Color.FromRgb(227, 242, 253)) : Brushes.White,
                            BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                            BorderThickness = new Thickness(0.5), Padding = new Thickness(4, 4, 4, 4),
                            Margin = new Thickness(1), MinHeight = 45, CornerRadius = new CornerRadius(2)
                        };
                        
                        if (lessonListForCell.Any())
                        {
                            var cellSp = new StackPanel();
                            foreach(var lesson in lessonListForCell)
                            {
                                var innerSp = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };
                                innerSp.Children.Add(new TextBlock { Text = lesson.Title, FontSize = 10, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
                                innerSp.Children.Add(new TextBlock { Text = $"{lesson.Subject} · Lớp {lesson.ClassName}", FontSize = 9, Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)) });
                                if (!string.IsNullOrEmpty(lesson.TeacherName))
                                    innerSp.Children.Add(new TextBlock { Text = $"👨‍🏫 {lesson.TeacherName}", FontSize = 9, Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)) });
                                if (!string.IsNullOrEmpty(lesson.SubstituteTeacher))
                                    innerSp.Children.Add(new TextBlock { Text = $"🔄 {lesson.SubstituteTeacher}", FontSize = 9, Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0)) });
                                cellSp.Children.Add(innerSp);
                            }
                            cell.Child = cellSp;
                        }
                        Grid.SetRow(cell, p); Grid.SetColumn(cell, d + 1); grid.Children.Add(cell);
                    }
                }
                gridContainer.Content = grid;
            };

            typeCb.SelectionChanged += (s, ev) => renderGrid();
            renderGrid();

            wnd.Content = new ScrollViewer { Content = mainSp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            wnd.ShowDialog();
        }

        // ═════════════════════════════════════════════════════
        //  HELPERS
        // ═════════════════════════════════════════════════════

        private static string GetStatusBg(string s) => s switch
        {
            "Approved"        => "#E8F5E9", "Taught" => "#E3F2FD",
            "PendingApproval" => "#FFF3E0", "Draft"  => "#FAFAFA", _ => "#F5F5F5"
        };
        private static string GetStatusFg(string s) => s switch
        {
            "Approved"        => "#2E7D32", "Taught" => "#1565C0",
            "PendingApproval" => "#E65100", "Draft"  => "#757575", _ => "#616161"
        };
        private static string GetStatusLabel(string s) => s switch
        {
            "Approved" => "✅ Đã duyệt", "Taught" => "📚 Đã dạy",
            "PendingApproval" => "⏳ Chờ duyệt", "Draft" => "📝 Bản nháp", _ => s
        };
        private static string GetSubjectIcon(string sub) => sub switch
        {
            "Toán"      => "📐", "Vật lý"     => "⚡", "Hóa học"   => "🧪",
            "Sinh học"  => "🌿", "Ngữ văn"    => "📖", "Tiếng Anh" => "🔤",
            "Lịch sử"   => "🏛️", "Địa lý"    => "🌍", _           => "📚"
        };
        private static string GetAccentBrush(string sub) => sub switch
        {
            "Toán"       => "#1976D2", "Vật lý"     => "#FF8F00", "Hóa học"   => "#00897B",
            "Sinh học"   => "#43A047", "Ngữ văn"    => "#8E24AA", "Tiếng Anh" => "#D32F2F",
            "Lịch sử"    => "#5D4037", "Địa lý"     => "#0097A7", _           => "#546E7A"
        };

        private static TextBlock MkLabel(string text) => new()
        {
            Text = text, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
            Margin = new Thickness(0, 0, 0, 3)
        };
    }

    // ─── ViewModel ─────────────────────────────────────────────
    public class LessonViewModel
    {
        public int      Id          { get; set; }
        public string   Title       { get; set; } = "";
        public string   Subject     { get; set; } = "";
        public string   Grade       { get; set; } = "";
        public string   Description { get; set; } = "";
        public string   Status      { get; set; } = "";
        public bool     IsFavorite  { get; set; }
        public int      UseCount    { get; set; }
        public DateTime UpdatedAt   { get; set; }
        public string   LessonType  { get; set; } = "";
        public int      DurationMin { get; set; }
        // Timetable
        public int      Period      { get; set; }
        public string   DayOfWeekStr{ get; set; } = "";
        public int      WeekNumber  { get; set; }
        public string   ClassName   { get; set; } = "";
        public string   TeacherName { get; set; } = "";
        public string   SubstituteTeacher { get; set; } = "";
        // Display
        public string StatusBg    { get; set; } = "";
        public string StatusFg    { get; set; } = "";
        public string StatusLabel { get; set; } = "";
        public string SubjectIcon { get; set; } = "📚";
        public string TypeIcon    { get; set; } = "📖";
        public string FavIcon     { get; set; } = "☆";
        public string UsedLabel   { get; set; } = "";
        public string CardBg      { get; set; } = "White";
        public string AccentBrush { get; set; } = "#1976D2";
        public string UpdatedLabel => UpdatedAt.ToString("dd/MM/yyyy");

        // Timetable display
        public string TimetableLabel => Period > 0 ? $"📅 {DayOfWeekStr} · Tiết {Period} · {ClassName}" : "";
        public string TeacherLabel => !string.IsNullOrEmpty(TeacherName)
            ? (string.IsNullOrEmpty(SubstituteTeacher) ? $"👨‍🏫 {TeacherName}" : $"🔄 {SubstituteTeacher} (thay {TeacherName})")
            : "";
        public System.Windows.Visibility TimetableVis => Period > 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        public System.Windows.Visibility TeacherVis => !string.IsNullOrEmpty(TeacherName) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    }
}
