using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.Data;
using QASmartClass.Classroom.Services;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class StudentPage : Page
    {
        private List<Student> _allStudents = new();
        private string _searchText = "";
        private string _filterMode = "all"; // all, online, offline
        private string _sortColumn = "FullName";
        private System.ComponentModel.ListSortDirection _sortDirection = System.ComponentModel.ListSortDirection.Ascending;
        private readonly bool _showRosterFirst;

        public StudentPage(bool showRosterFirst = false)
        {
            _showRosterFirst = showRosterFirst;
            InitializeComponent();
            Loaded += (_, _) =>
            {
                var app = (QASmartTouch.App)Application.Current;
                if (app != null)
                {
                    app.NetworkService.MessageReceived += NetworkService_MessageReceived;
                    app.NetworkService.StudentConnected += NetworkService_StudentConnected;
                    app.NetworkService.StudentDisconnected += NetworkService_StudentDisconnected;
                }
                PopulateClassFilter();
                LoadStudents();
                // Auto-expand roster panel when navigating from "Danh sách lớp" menu
                if (_showRosterFirst)
                {
                    rosterExpander.IsExpanded = true;
                    LoadRosters();
                }
            };
            Unloaded += (_, _) =>
            {
                var app = (QASmartTouch.App)Application.Current;
                if (app != null)
                {
                    app.NetworkService.MessageReceived -= NetworkService_MessageReceived;
                    app.NetworkService.StudentConnected -= NetworkService_StudentConnected;
                    app.NetworkService.StudentDisconnected -= NetworkService_StudentDisconnected;
                }
            };

            PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    txtSearch.Focus();
                    txtSearch.SelectAll();
                    e.Handled = true;
                }
                else if (e.Key == Key.F5)
                {
                    LoadStudents();
                    e.Handled = true;
                }
            };
            studentDataGrid.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Delete)
                {
                    if (e.IsRepeat)
                    {
                        e.Handled = true;
                        return;
                    }
                    DeleteStudent_Click(s, null);
                    e.Handled = true;
                }
            };
        }

        private async void LoadStudents()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var activeRoster = app.ClassRoster.ActiveRoster;
                List<Student> students = null;

                await Task.Run(() =>
                {
                    if (activeRoster != null)
                    {
                        students = app.ClassRoster.GetActiveStudents();
                    }
                    else
                    {
                        students = VietnameseNameHelper.SortByVietnameseName(
                            app.Database.Students.ToList(), s => s.FullName);
                    }
                });

                _allStudents = students ?? new List<Student>();

                if (activeRoster != null)
                    txtSubtitle.Text = $"Danh sách học sinh — {activeRoster.DisplayName} ({_allStudents.Count} HS)";
                else
                    txtSubtitle.Text = $"Danh sách học sinh — Tất cả ({_allStudents.Count} HS)";

                if (!_allStudents.Any())
                {
                    // Fallback demo
                    _allStudents = GenerateDemoStudents();
                }

                UpdateStats();
                RefreshGrid();
                RenderSeatingChart();
                Log.Information("StudentPage loaded {Count} students", _allStudents.Count);
            }
            catch (Exception ex)
            {
                Log.Warning("StudentPage load error: {Error}", ex.Message);
                _allStudents = GenerateDemoStudents();
                UpdateStats();
                RefreshGrid();
            }
        }

        private List<Student> GenerateDemoStudents()
        {
            // Sắp xếp theo chuẩn VN: Tên → Họ
            var names = new[] {
                "Nguyễn Văn An","Trần Thị Bình","Lê Hoàng Cường","Phạm Thị Dung",
                "Hoàng Văn Em","Đỗ Quang Hải","Vũ Thị Hoa","Bùi Đức Khang",
                "Lý Thị Lan","Mai Văn Minh","Đinh Thị Ngọc","Trương Quốc Phong",
                "Ngô Thị Phương","Đặng Thị Quỳnh","Hà Sĩ Ren","Cao Thị Sen",
                "Tô Văn Tuấn","Phan Thị Uyên","Lương Văn Vũ","Châu Thị Xuân"
            };
            return names.Select((n, i) => new Student
            {
                Id = i + 1, FullName = n, StudentCode = $"HS{i + 1:D3}",
                PCName = $"PC-{i + 1:D2}", IPAddress = $"192.168.1.{100 + i}",
                IsOnline = i % 7 != 0, LastSeen = DateTime.Now.AddMinutes(-i * 3)
            }).ToList();
        }

        private void UpdateStats()
        {
            int total = _allStudents.Count;
            int online = _allStudents.Count(s => s.IsOnline);
            int offline = total - online;
            txtTotalCount.Text = total.ToString();
            txtOnlineCount.Text = online.ToString();
            txtOfflineCount.Text = offline.ToString();
        }

        private void RefreshGrid()
        {
            if (studentDataGrid == null) return;

            var filtered = _allStudents.AsEnumerable();

            // Apply filter mode
            if (_filterMode == "online") filtered = filtered.Where(s => s.IsOnline);
            else if (_filterMode == "offline") filtered = filtered.Where(s => !s.IsOnline);

            // Apply search
            if (!string.IsNullOrWhiteSpace(_searchText))
                filtered = filtered.Where(s =>
                    s.FullName.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                    s.StudentCode.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                    s.PCName.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

            // Apply sorting
            var list = filtered.ToList();
            bool asc = _sortDirection == System.ComponentModel.ListSortDirection.Ascending;

            list = _sortColumn switch
            {
                "FullName" => asc
                    ? VietnameseNameHelper.SortByVietnameseName(list, s => s.FullName)
                    : VietnameseNameHelper.SortByVietnameseName(list, s => s.FullName)
                           .AsEnumerable().Reverse().ToList(),
                "StudentCode" => asc ? list.OrderBy(s => s.StudentCode).ToList() : list.OrderByDescending(s => s.StudentCode).ToList(),
                "PCName" => asc ? list.OrderBy(s => s.PCName).ToList() : list.OrderByDescending(s => s.PCName).ToList(),
                "IPAddress" => asc ? list.OrderBy(s => s.IPAddress).ToList() : list.OrderByDescending(s => s.IPAddress).ToList(),
                "IsOnline" => asc ? list.OrderBy(s => s.IsOnline).ThenBy(s => s.FullName).ToList() : list.OrderByDescending(s => s.IsOnline).ThenBy(s => s.FullName).ToList(),
                "LastSeen" => asc ? list.OrderBy(s => s.LastSeen).ToList() : list.OrderByDescending(s => s.LastSeen).ToList(),
                _ => VietnameseNameHelper.SortByVietnameseName(list, s => s.FullName)
            };

            studentDataGrid.ItemsSource = list;
        }

        /// <summary>Custom sorting handler — Vietnamese name sorting by last word</summary>
        private void StudentGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true; // prevent default sort

            var col = e.Column.SortMemberPath;
            if (string.IsNullOrEmpty(col)) return;

            // Toggle direction
            if (_sortColumn == col)
                _sortDirection = _sortDirection == System.ComponentModel.ListSortDirection.Ascending
                    ? System.ComponentModel.ListSortDirection.Descending
                    : System.ComponentModel.ListSortDirection.Ascending;
            else
            {
                _sortColumn = col;
                _sortDirection = System.ComponentModel.ListSortDirection.Ascending;
            }

            // Update column header direction indicator
            foreach (var column in studentDataGrid.Columns)
                column.SortDirection = null;
            e.Column.SortDirection = _sortDirection;

            RefreshGrid();
            Log.Information("StudentPage sorted by {Column} {Dir}", _sortColumn, _sortDirection);
        }

        // ═══════════════════════════════════════════════════════════
        //  SEARCH & FILTER
        // ═══════════════════════════════════════════════════════════

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtSearch == null) return;
            _searchText = txtSearch.Text.Trim();
            if (_searchText == "🔍 Tìm kiếm học sinh...") _searchText = "";
            RefreshGrid();
        }

        private void Search_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text.StartsWith("🔍"))
            {
                txtSearch.Text = "";
                txtSearch.Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33));
            }
        }

        private void Search_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = "🔍 Tìm kiếm học sinh...";
                txtSearch.Foreground = new SolidColorBrush(Color.FromRgb(189, 189, 189));
            }
        }

        /// <summary>Populate class filter ComboBox with all rosters</summary>
        private void PopulateClassFilter()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var rosters = app.ClassRoster.GetAllRosters();
                
                cmbClassFilter.SelectionChanged -= ClassFilter_Changed; // Ngăn chặn loop
                cmbClassFilter.Items.Clear();
                
                var allItem = new ComboBoxItem { Content = "📋 Tất cả lớp", Tag = "ALL" };
                cmbClassFilter.Items.Add(allItem);
                
                var activeRoster = app.ClassRoster.ActiveRoster;
                ComboBoxItem selectedItem = allItem;
                
                foreach (var r in rosters)
                {
                    var item = new ComboBoxItem
                    {
                        Content = $"{r.ClassName} — {r.Subject} ({r.StudentCount} HS)",
                        Tag = r.Id.ToString()
                    };
                    if (activeRoster != null && activeRoster.Id == r.Id)
                    {
                        selectedItem = item;
                    }
                    cmbClassFilter.Items.Add(item);
                }
                
                cmbClassFilter.SelectedItem = selectedItem;
                cmbClassFilter.SelectionChanged += ClassFilter_Changed;
            }
            catch (Exception ex) { Log.Warning("PopulateClassFilter error: {Err}", ex.Message); }
        }

        /// <summary>When user selects a class from the filter ComboBox</summary>
        private void ClassFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (cmbClassFilter?.SelectedItem is not ComboBoxItem selected) return;
            var tag = selected.Tag?.ToString() ?? "ALL";

            try
            {
                var app = (QASmartTouch.App)Application.Current;
                if (tag == "ALL")
                {
                    app.ClassRoster.SetActiveRoster(null); // Giải phóng active roster tránh lỗi reload
                    _allStudents = VietnameseNameHelper.SortByVietnameseName(
                        app.Database.Students.ToList(), s => s.FullName);
                    txtSubtitle.Text = $"Danh sách học sinh — Tất cả ({_allStudents.Count} HS)";
                }
                else if (int.TryParse(tag, out int rosterId))
                {
                    var roster = app.ClassRoster.GetAllRosters().FirstOrDefault(r => r.Id == rosterId);
                    if (roster != null)
                    {
                        app.ClassRoster.SetActiveRoster(roster);
                        _allStudents = app.ClassRoster.GetActiveStudents();
                        txtSubtitle.Text = $"Danh sách học sinh — {roster.DisplayName} ({_allStudents.Count} HS)";
                    }
                }
                UpdateStats();
                RefreshGrid();
            }
            catch (Exception ex) { Log.Warning("ClassFilter error: {Err}", ex.Message); }
        }

        private void SelectClassFilterItem(int? rosterId)
        {
            if (cmbClassFilter == null) return;
            string targetTag = rosterId?.ToString() ?? "ALL";
            foreach (ComboBoxItem item in cmbClassFilter.Items)
            {
                if (item.Tag?.ToString() == targetTag)
                {
                    cmbClassFilter.SelectedItem = item;
                    break;
                }
            }
        }

        private void UpdateFilterButtons()
        {
            btnFilterAll.Background = _filterMode == "all" ? new SolidColorBrush(Color.FromRgb(227, 242, 253)) : new SolidColorBrush(Color.FromRgb(245, 245, 245));
            btnFilterOnline.Background = _filterMode == "online" ? new SolidColorBrush(Color.FromRgb(232, 245, 233)) : new SolidColorBrush(Color.FromRgb(245, 245, 245));
            btnFilterOffline.Background = _filterMode == "offline" ? new SolidColorBrush(Color.FromRgb(255, 235, 238)) : new SolidColorBrush(Color.FromRgb(245, 245, 245));
        }

        private void FilterAll_Click(object sender, RoutedEventArgs e) { _filterMode = "all"; UpdateFilterButtons(); RefreshGrid(); }
        private void FilterOnline_Click(object sender, RoutedEventArgs e) { _filterMode = "online"; UpdateFilterButtons(); RefreshGrid(); }
        private void FilterOffline_Click(object sender, RoutedEventArgs e) { _filterMode = "offline"; UpdateFilterButtons(); RefreshGrid(); }

        // ═══════════════════════════════════════════════════════════
        //  ADD STUDENT
        // ═══════════════════════════════════════════════════════════

        private void AddStudent_Click(object sender, RoutedEventArgs e)
        {
            ShowStudentEditor(null);
        }

        // ═══════════════════════════════════════════════════════════
        //  EDIT STUDENT
        // ═══════════════════════════════════════════════════════════

        private void EditStudent_Click(object sender, RoutedEventArgs e)
        {
            if (studentDataGrid.SelectedItem is Student s)
                ShowStudentEditor(s);
            else
                MessageBox.Show("Vui lòng chọn 1 học sinh trước!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void StudentGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (studentDataGrid.SelectedItem is Student s)
                ShowStudentEditor(s);
        }

        /// <summary>Dialog thêm mới hoặc sửa học sinh</summary>
        private void ShowStudentEditor(Student? existing)
        {
            bool isNew = existing == null;
            var wnd = new Window
            {
                Title = isNew ? "➕ Thêm học sinh mới" : $"✏️ Sửa: {existing!.FullName}",
                Width = 460, Height = 480,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this), ResizeMode = ResizeMode.NoResize,
                Background = Brushes.White
            };
            var sp = new StackPanel { Margin = new Thickness(20, 16, 20, 16) };

            // Title
            sp.Children.Add(new TextBlock
            {
                Text = isNew ? "👨‍🎓 Thêm học sinh mới" : "✏️ Chỉnh sửa thông tin",
                FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 14),
                Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33))
            });

            // Lấy mã HS, số máy, IP lớn nhất hiện tại ngoài vòng lặp để tránh trùng lặp trùng mã
            int maxStudentNum = 0;
            int maxPCNum = 0;
            int maxIPNum = 100;
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var allDbStudents = app.Database.Students.ToList();
                foreach (var s in allDbStudents)
                {
                    if (s.StudentCode != null && s.StudentCode.StartsWith("HS"))
                    {
                        if (int.TryParse(s.StudentCode.Substring(2), out int num) && num > maxStudentNum)
                            maxStudentNum = num;
                    }
                    if (s.PCName != null && s.PCName.StartsWith("PC-"))
                    {
                        if (int.TryParse(s.PCName.Substring(3), out int num) && num > maxPCNum)
                            maxPCNum = num;
                    }
                    if (s.IPAddress != null && s.IPAddress.StartsWith("192.168.1."))
                    {
                        if (int.TryParse(s.IPAddress.Substring(10), out int num) && num > maxIPNum && num < 255)
                            maxIPNum = num;
                    }
                }
            }
            catch { }
            int nextStudentNum = maxStudentNum + 1;
            int nextPCNum = maxPCNum + 1;
            int nextIPNum = maxIPNum + 1;
            if (nextIPNum >= 255) nextIPNum = 101;

            // ── Row 1: Full name ──
            sp.Children.Add(MakeLabel("👤 Họ và tên: *"));
            var tbName = new TextBox
            {
                Text = existing?.FullName ?? "", FontSize = 13,
                Padding = new Thickness(10, 7, 10, 7), Margin = new Thickness(0, 0, 0, 10),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)), BorderThickness = new Thickness(1)
            };
            sp.Children.Add(tbName);

            // ── Row 2: Student code + PC name ──
            var row2 = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            row2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12, GridUnitType.Pixel) });
            row2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var codePanel = new StackPanel();
            codePanel.Children.Add(MakeLabel("🆔 Mã HS:"));
            var tbCode = new TextBox
            {
                Text = existing?.StudentCode ?? $"HS{nextStudentNum:D3}", FontSize = 12,
                Padding = new Thickness(8, 6, 8, 6),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)), BorderThickness = new Thickness(1)
            };
            codePanel.Children.Add(tbCode);
            Grid.SetColumn(codePanel, 0); row2.Children.Add(codePanel);

            var pcPanel = new StackPanel();
            pcPanel.Children.Add(MakeLabel("💻 Máy tính:"));
            var tbPC = new TextBox
            {
                Text = existing?.PCName ?? $"PC-{nextPCNum:D2}", FontSize = 12,
                Padding = new Thickness(8, 6, 8, 6),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)), BorderThickness = new Thickness(1)
            };
            pcPanel.Children.Add(tbPC);
            Grid.SetColumn(pcPanel, 2); row2.Children.Add(pcPanel);
            sp.Children.Add(row2);

            // ── Row 3: IP + Status ──
            var row3 = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            row3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12, GridUnitType.Pixel) });
            row3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var ipPanel = new StackPanel();
            ipPanel.Children.Add(MakeLabel("🌐 Địa chỉ IP:"));
            var tbIP = new TextBox
            {
                Text = existing?.IPAddress ?? $"192.168.1.{nextIPNum}", FontSize = 12,
                Padding = new Thickness(8, 6, 8, 6),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)), BorderThickness = new Thickness(1)
            };
            ipPanel.Children.Add(tbIP);
            Grid.SetColumn(ipPanel, 0); row3.Children.Add(ipPanel);

            var statusPanel = new StackPanel();
            statusPanel.Children.Add(MakeLabel("📡 Trạng thái:"));
            var cmbStatus = new ComboBox { FontSize = 12, Padding = new Thickness(6, 5, 6, 5), SelectedIndex = (existing?.IsOnline ?? true) ? 0 : 1 };
            cmbStatus.Items.Add(new ComboBoxItem { Content = "🟢 Online" });
            cmbStatus.Items.Add(new ComboBoxItem { Content = "⚫ Offline" });
            statusPanel.Children.Add(cmbStatus);
            Grid.SetColumn(statusPanel, 2); row3.Children.Add(statusPanel);
            sp.Children.Add(row3);

            // ── Row 4: Notes ──
            sp.Children.Add(MakeLabel("📝 Ghi chú (tùy chọn):"));
            var tbNotes = new TextBox
            {
                FontSize = 12, Padding = new Thickness(8, 6, 8, 6), AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap, MinHeight = 45, Margin = new Thickness(0, 0, 0, 14),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)), BorderThickness = new Thickness(1)
            };
            sp.Children.Add(tbNotes);

            // ── Buttons ──
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnCancel = new Button
            {
                Content = "Hủy", Width = 90, Height = 36, FontSize = 13,
                Margin = new Thickness(0, 0, 8, 0), Cursor = Cursors.Hand,
                IsCancel = true
            };
            btnCancel.Click += (s, e2) => wnd.Close();

            var btnSave = new Button
            {
                Content = isNew ? "✅ Thêm học sinh" : "✅ Lưu thay đổi",
                Width = 140, Height = 36, FontSize = 13,
                Background = new SolidColorBrush(Color.FromRgb(0, 105, 92)),
                Foreground = Brushes.White, BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                IsDefault = true
            };
            btnSave.Click += async (s, e2) =>
            {
                if (string.IsNullOrWhiteSpace(tbName.Text))
                {
                    MessageBox.Show("Vui lòng nhập họ tên!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var ipText = tbIP.Text.Trim();
                if (!string.IsNullOrEmpty(ipText))
                {
                    var ipRegex = new System.Text.RegularExpressions.Regex(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");
                    if (!ipRegex.IsMatch(ipText))
                    {
                        MessageBox.Show("Địa chỉ IP không hợp lệ! Vui lòng nhập đúng định dạng IPv4 (ví dụ: 192.168.1.100).", 
                            "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    var studentName = tbName.Text.Trim();
                    var studentCode = tbCode.Text.Trim();
                    int studentId = isNew ? 0 : existing!.Id;

                    // ── Duplicate code check (áp dụng cho cả Thêm và Sửa) ──
                    Student? dupCode = null;
                    await Task.Run(() => {
                        dupCode = app.Database.Students.FirstOrDefault(x => x.StudentCode == studentCode && (isNew || x.Id != studentId));
                    });
                    if (dupCode != null)
                    {
                        MessageBox.Show($"Mã HS \"{studentCode}\" đã tồn tại!\nHS hiện tại: {dupCode.FullName}\n\nVui lòng nhập mã khác.",
                            "⚠️ Trùng mã HS", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // ── Cảnh báo trùng PCName ──
                    string pcName = tbPC.Text.Trim();
                    Student? dupPC = null;
                    if (!string.IsNullOrWhiteSpace(pcName))
                    {
                        await Task.Run(() => {
                            dupPC = app.Database.Students.FirstOrDefault(x => x.PCName == pcName && (isNew || x.Id != studentId));
                        });
                        if (dupPC != null)
                        {
                            var r = MessageBox.Show(
                                $"Tên máy tính \"{pcName}\" đã được gán cho học sinh: {dupPC.FullName} (Mã: {dupPC.StudentCode}).\n\nBạn vẫn muốn tiếp tục sử dụng tên máy tính này?",
                                "⚠️ Trùng tên máy tính", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (r != MessageBoxResult.Yes) return;
                        }
                    }

                    // ── Cảnh báo trùng IPAddress ──
                    Student? dupIP = null;
                    if (!string.IsNullOrWhiteSpace(ipText))
                    {
                        await Task.Run(() => {
                            dupIP = app.Database.Students.FirstOrDefault(x => x.IPAddress == ipText && (isNew || x.Id != studentId));
                        });
                        if (dupIP != null)
                        {
                            var r = MessageBox.Show(
                                $"Địa chỉ IP \"{ipText}\" đã được gán cho học sinh: {dupIP.FullName} (Mã: {dupIP.StudentCode}).\n\nBạn vẫn muốn tiếp tục sử dụng địa chỉ IP này?",
                                "⚠️ Trùng địa chỉ IP", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (r != MessageBoxResult.Yes) return;
                        }
                    }

                    if (isNew)
                    {
                        var student = new Student
                        {
                            FullName = studentName,
                            StudentCode = studentCode,
                            PCName = pcName,
                            IPAddress = ipText,
                            IsOnline = cmbStatus.SelectedIndex == 0,
                            LastSeen = DateTime.Now
                        };
                        app.Database.Students.Add(student);
                    }
                    else
                    {
                        existing!.FullName = studentName;
                        existing.StudentCode = studentCode;
                        existing.PCName = pcName;
                        existing.IPAddress = ipText;
                        existing.IsOnline = cmbStatus.SelectedIndex == 0;
                    }

                    await Task.Run(() => app.Database.SaveChanges()); // Lưu DB bất đồng bộ hoàn toàn
                    wnd.Close();
                    LoadStudents();
                    MessageBox.Show(isNew ? "✅ Đã thêm học sinh!" : "✅ Đã cập nhật!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            btnPanel.Children.Add(btnCancel);
            btnPanel.Children.Add(btnSave);
            sp.Children.Add(btnPanel);

            wnd.Content = new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            wnd.ContentRendered += (s, e2) =>
            {
                tbName.Focus();
                if (!isNew)
                {
                    tbName.SelectAll();
                }
            };
            wnd.ShowDialog();
        }

        // ═══════════════════════════════════════════════════════════
        //  DELETE STUDENT
        // ═══════════════════════════════════════════════════════════

        private async void DeleteStudent_Click(object sender, RoutedEventArgs e)
        {
            if (studentDataGrid.SelectedItem is Student s)
            {
                var r = MessageBox.Show($"Xóa học sinh: \"{s.FullName}\" ({s.StudentCode})?\nTất cả liên kết lớp học của học sinh này sẽ bị xóa bỏ hoàn toàn.",
                    "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (r == MessageBoxResult.Yes)
                {
                    try
                    {
                        var app = (QASmartTouch.App)Application.Current;
                        await Task.Run(() =>
                        {
                            // 1. Tìm và xóa liên kết lớp học tránh dữ liệu mồ côi (Orphan rows)
                            var links = app.Database.ClassRosterStudents.Where(rs => rs.StudentId == s.Id).ToList();
                            if (links.Any())
                                app.Database.ClassRosterStudents.RemoveRange(links);

                            // 2. Xóa học sinh chính
                            app.Database.Students.Remove(s);
                            app.Database.SaveChanges();
                        });
                        LoadStudents();
                        MessageBox.Show("✅ Đã xóa học sinh và các liên kết liên quan thành công.", "Thành công");
                    }
                    catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
                }
            }
            else MessageBox.Show("Vui lòng chọn 1 học sinh trước!", "Thông báo");
        }

        // ═══════════════════════════════════════════════════════════
        //  IMPORT (CSV)
        // ═══════════════════════════════════════════════════════════

        private async void ImportExcel_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Nhập danh sách học sinh từ Excel hoặc CSV",
                Filter = "Excel & CSV Files (*.xlsx;*.csv)|*.xlsx;*.csv|Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var filePath = dlg.FileName;
                var app = (QASmartTouch.App)Application.Current;

                var result = await Task.Run(() =>
                {
                    System.Data.DataTable dt;
                    if (filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        dt = QASmartClass.Services.ExcelDataService.ReadExcelToDataTable(filePath);
                    }
                    else if (filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        dt = ParseStudentCsvToDataTable(filePath);
                    }
                    else
                    {
                        return (Success: false, Count: 0, Message: "Định dạng file không được hỗ trợ!", Warnings: new List<string>());
                    }

                    if (dt == null || dt.Rows.Count == 0)
                    {
                        return (Success: false, Count: 0, Message: "File dữ liệu không có dòng thông tin hợp lệ nào!", Warnings: new List<string>());
                    }

                    int count = 0;
                    int maxStudentNum = 0;
                    int maxPCNum = 0;
                    int maxIPNum = 100;
                    try
                    {
                        var allDbStudents = app.Database.Students.ToList();
                        foreach (var s in allDbStudents)
                        {
                            if (s.StudentCode != null && s.StudentCode.StartsWith("HS"))
                            {
                                if (int.TryParse(s.StudentCode.Substring(2), out int num) && num > maxStudentNum)
                                    maxStudentNum = num;
                            }
                            if (s.PCName != null && s.PCName.StartsWith("PC-"))
                            {
                                if (int.TryParse(s.PCName.Substring(3), out int num) && num > maxPCNum)
                                    maxPCNum = num;
                            }
                            if (s.IPAddress != null && s.IPAddress.StartsWith("192.168.1."))
                            {
                                if (int.TryParse(s.IPAddress.Substring(10), out int num) && num > maxIPNum && num < 255)
                                    maxIPNum = num;
                            }
                        }
                    }
                    catch { }

                    var ipRegex = new System.Text.RegularExpressions.Regex(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");
                    var ipWarnings = new List<string>();
                    int rowNum = 1;

                    using var transaction = app.Database.Database.BeginTransaction();
                    try
                    {
                        foreach (System.Data.DataRow row in dt.Rows)
                        {
                            rowNum++;
                            string fullName = dt.Columns.Count > 0 ? row[0]?.ToString()?.Trim() ?? "" : "";
                            if (string.IsNullOrWhiteSpace(fullName)) continue;

                            string studentCode = dt.Columns.Count > 1 && !string.IsNullOrWhiteSpace(row[1]?.ToString()) 
                                ? row[1].ToString().Trim() 
                                : $"HS{(++maxStudentNum):D3}";
                            string pcName = dt.Columns.Count > 2 && !string.IsNullOrWhiteSpace(row[2]?.ToString()) 
                                ? row[2].ToString().Trim() 
                                : $"PC-{(++maxPCNum):D2}";
                            
                            string ipAddress = "";
                            if (dt.Columns.Count > 3 && !string.IsNullOrWhiteSpace(row[3]?.ToString()))
                            {
                                var tempIp = row[3].ToString().Trim();
                                if (ipRegex.IsMatch(tempIp))
                                {
                                    ipAddress = tempIp;
                                }
                                else
                                {
                                    ipAddress = "";
                                    ipWarnings.Add($"Dòng {rowNum} (Học sinh: {fullName}): Địa chỉ IP '{tempIp}' sai định dạng IPv4.");
                                }
                            }

                            if (string.IsNullOrEmpty(ipAddress))
                            {
                                maxIPNum++;
                                if (maxIPNum >= 255) maxIPNum = 101;
                                ipAddress = $"192.168.1.{maxIPNum}";
                            }

                            // Kiểm tra trùng lặp PCName trong bộ nhớ DB hiện tại
                            var dupPC = app.Database.Students.Any(s => s.PCName == pcName);
                            if (dupPC) pcName = $"PC-{(++maxPCNum):D2}";

                            // Kiểm tra trùng lặp IPAddress trong bộ nhớ DB hiện tại
                            var dupIP = app.Database.Students.Any(s => s.IPAddress == ipAddress);
                            if (dupIP && !string.IsNullOrEmpty(ipAddress))
                            {
                                maxIPNum++;
                                if (maxIPNum >= 255) maxIPNum = 101;
                                ipAddress = $"192.168.1.{maxIPNum}";
                            }

                            var existingStudent = app.Database.Students.FirstOrDefault(s => s.StudentCode == studentCode);
                            if (existingStudent == null)
                            {
                                // Sinh mật khẩu mặc định băm HMACSHA512 theo chuẩn v4.1
                                string defaultPwd = $"Hs@{studentCode.Substring(Math.Max(0, studentCode.Length - 4))}";
                                string pwdHash = QASmartTouch.Services.AuthenticationService.HashPasswordHMACSHA512(defaultPwd);

                                var student = new Student
                                {
                                    FullName = fullName,
                                    StudentCode = studentCode,
                                    PCName = pcName,
                                    IPAddress = ipAddress,
                                    PasswordHash = pwdHash,
                                    IsOnline = false,
                                    LastSeen = DateTime.Now
                                };
                                app.Database.Students.Add(student);
                                count++;
                            }
                        }
                        app.Database.SaveChanges();
                        transaction.Commit();
                        return (Success: true, Count: count, Message: $"Đã import {count} học sinh thành công!", Warnings: ipWarnings);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return (Success: false, Count: 0, Message: $"Lỗi lưu cơ sở dữ liệu: {ex.Message}", Warnings: new List<string>());
                    }
                });

                if (result.Success)
                {
                    LoadStudents();
                    string msg = result.Message;
                    if (result.Warnings != null && result.Warnings.Any())
                    {
                        msg += $"\n\n⚠️ Lưu ý cảnh báo IP:\n" + string.Join("\n", result.Warnings.Take(5));
                        if (result.Warnings.Count > 5)
                        {
                            msg += $"\n... và {result.Warnings.Count - 5} cảnh báo khác.";
                        }
                    }
                    MessageBox.Show(msg, "Kết quả Import", MessageBoxButton.OK, 
                        result.Warnings.Any() ? MessageBoxImage.Warning : MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(result.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi import: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static System.Data.DataTable ParseStudentCsvToDataTable(string filePath)
        {
            var table = new System.Data.DataTable();
            table.Columns.Add("Họ tên");
            table.Columns.Add("Mã HS");
            table.Columns.Add("Tên PC");
            table.Columns.Add("Địa chỉ IP");

            var lines = File.ReadAllLines(filePath).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            if (lines.Length <= 1) return table;

            foreach (var line in lines.Skip(1))
            {
                var cols = QASmartClass.Classroom.Services.ClassRosterService.ParseCsvLine(line);
                var row = table.NewRow();
                for (int col = 0; col < Math.Min(cols.Length, 4); col++)
                {
                    row[col] = cols[col];
                }
                table.Rows.Add(row);
            }
            return table;
        }

        // ═══════════════════════════════════════════════════════════
        //  EXPORT (CSV)
        // ═══════════════════════════════════════════════════════════

        private async void ExportList_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Xuất danh sách học sinh",
                Filter = "CSV|*.csv",
                FileName = $"DanhSachHS_{DateTime.Now:yyyyMMdd}.csv"
            };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var filePath = dlg.FileName;
                    var studentsToExport = _allStudents.ToList();

                    await Task.Run(() =>
                    {
                        var lines = new List<string> { "Họ tên,Mã HS,Máy tính,IP,Online,Lần cuối" };
                        foreach (var s in studentsToExport)
                        {
                            var name = ClassRosterService.SanitizeCsvField(s.FullName);
                            var code = ClassRosterService.SanitizeCsvField(s.StudentCode);
                            var pc = ClassRosterService.SanitizeCsvField(s.PCName);
                            var ip = ClassRosterService.SanitizeCsvField(s.IPAddress);
                            var online = s.IsOnline ? "Có" : "Không";
                            var lastSeen = s.LastSeen.ToString("HH:mm dd/MM/yyyy");

                            lines.Add($"\"{name}\",\"{code}\",\"{pc}\",\"{ip}\",\"{online}\",\"{lastSeen}\"");
                        }
                        File.WriteAllLines(filePath, lines, System.Text.Encoding.UTF8);
                    });

                    MessageBox.Show($"✅ Đã xuất {studentsToExport.Count} học sinh ra CSV!", "Export thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    Log.Information("Exported {Count} students to CSV", studentsToExport.Count);
                }
                catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  REFRESH
        // ═══════════════════════════════════════════════════════════

        private void RefreshList_Click(object sender, RoutedEventArgs e)
        {
            LoadStudents();
            Log.Information("StudentPage refreshed");
        }

        private static TextBlock MakeLabel(string text) => new()
        {
            Text = text, FontSize = 12, FontWeight = FontWeights.SemiBold, 
            Foreground = new SolidColorBrush(Color.FromRgb(55, 71, 79)),
            Margin = new Thickness(0, 4, 0, 4)
        };

        // ═══════════════════════════════════════════════════════════
        //  CLASS ROSTER MANAGEMENT
        // ═══════════════════════════════════════════════════════════

        private void RosterExpander_Expanded(object sender, RoutedEventArgs e)
        {
            LoadRosters();
        }

        private async void LoadRosters()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var rosters = await Task.Run(() => app.ClassRoster.GetAllRosters());
                rosterDataGrid.ItemsSource = rosters;
                txtRosterCount.Text = $"{rosters.Count} lớp";
                Log.Information("Loaded {Count} rosters", rosters.Count);
            }
            catch (Exception ex)
            {
                Log.Warning("LoadRosters error: {Err}", ex.Message);
            }
        }

        private void RefreshRosters_Click(object sender, RoutedEventArgs e) => LoadRosters();

        private void CreateRoster_Click(object sender, RoutedEventArgs e)
        {
            ShowRosterEditor(null);
        }

        private void EditRoster_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is Data.ClassRoster roster)
                ShowRosterEditor(roster);
        }

        private void RosterGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (rosterDataGrid.SelectedItem is Data.ClassRoster roster)
                ShowRosterEditor(roster);
        }

        private void ShowRosterEditor(Data.ClassRoster? existing)
        {
            bool isNew = existing == null;
            var wnd = new Window
            {
                Title = isNew ? "➕ Tạo danh sách lớp mới" : $"✏️ Sửa: {existing!.ClassName}",
                Width = 440, Height = 420,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this), ResizeMode = ResizeMode.NoResize,
                Background = Brushes.White
            };
            var sp = new StackPanel { Margin = new Thickness(20, 16, 20, 16) };

            sp.Children.Add(new TextBlock
            {
                Text = isNew ? "📋 Tạo danh sách lớp mới" : "✏️ Chỉnh sửa lớp",
                FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 14),
                Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33))
            });

            // Row 1: Tên lớp + Khối
            var row1 = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            row1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12, GridUnitType.Pixel) });
            row1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80, GridUnitType.Pixel) });

            var classPanel = new StackPanel();
            classPanel.Children.Add(MakeLabel("📚 Tên lớp: *"));
            var tbClass = new TextBox { Text = existing?.ClassName ?? "", FontSize = 13, Padding = new Thickness(8, 6, 8, 6), BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)), BorderThickness = new Thickness(1) };
            classPanel.Children.Add(tbClass);
            Grid.SetColumn(classPanel, 0); row1.Children.Add(classPanel);

            var gradePanel = new StackPanel();
            gradePanel.Children.Add(MakeLabel("📐 Khối:"));
            var cmbGrade = new ComboBox { FontSize = 12, Padding = new Thickness(4, 4, 4, 4) };
            foreach (var g in new[] { "10", "11", "12", "6", "7", "8", "9" })
                cmbGrade.Items.Add(new ComboBoxItem { Content = g, IsSelected = g == (existing?.GradeLevel ?? "10") });
            gradePanel.Children.Add(cmbGrade);
            Grid.SetColumn(gradePanel, 2); row1.Children.Add(gradePanel);
            sp.Children.Add(row1);

            // Row 2: Môn + GV
            var row2 = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            row2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12, GridUnitType.Pixel) });
            row2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var subPanel = new StackPanel();
            subPanel.Children.Add(MakeLabel("📖 Môn học:"));
            var tbSubject = new TextBox { Text = existing?.Subject ?? "", FontSize = 12, Padding = new Thickness(8, 6, 8, 6), BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)), BorderThickness = new Thickness(1) };
            subPanel.Children.Add(tbSubject);
            Grid.SetColumn(subPanel, 0); row2.Children.Add(subPanel);

            var teacherPanel = new StackPanel();
            teacherPanel.Children.Add(MakeLabel("👨‍🏫 Giáo viên:"));
            var tbTeacher = new TextBox { Text = existing?.TeacherName ?? "", FontSize = 12, Padding = new Thickness(8, 6, 8, 6), BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)), BorderThickness = new Thickness(1) };
            teacherPanel.Children.Add(tbTeacher);
            Grid.SetColumn(teacherPanel, 2); row2.Children.Add(teacherPanel);
            sp.Children.Add(row2);

            // Row 3: Năm học + HK
            var row3 = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            row3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12, GridUnitType.Pixel) });
            row3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80, GridUnitType.Pixel) });

            int currentYear = DateTime.Now.Year;
            int currentMonth = DateTime.Now.Month;
            string defaultYear = (currentMonth >= 6) ? $"{currentYear}-{currentYear + 1}" : $"{currentYear - 1}-{currentYear}";
            string defaultSem = (currentMonth >= 9 || currentMonth < 2) ? "HK1" : "HK2";
            string selectedYear = existing?.SchoolYear ?? defaultYear;
            string selectedSem = existing?.Semester ?? defaultSem;

            var yearPanel = new StackPanel();
            yearPanel.Children.Add(MakeLabel("📅 Năm học:"));
            var tbYear = new TextBox { Text = selectedYear, FontSize = 12, Padding = new Thickness(8, 6, 8, 6), BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)), BorderThickness = new Thickness(1) };
            yearPanel.Children.Add(tbYear);
            Grid.SetColumn(yearPanel, 0); row3.Children.Add(yearPanel);

            var semPanel = new StackPanel();
            semPanel.Children.Add(MakeLabel("🗓️ Học kỳ:"));
            var cmbSem = new ComboBox { FontSize = 12, Padding = new Thickness(4, 4, 4, 4) };
            cmbSem.Items.Add(new ComboBoxItem { Content = "HK1", IsSelected = selectedSem == "HK1" });
            cmbSem.Items.Add(new ComboBoxItem { Content = "HK2", IsSelected = selectedSem == "HK2" });
            semPanel.Children.Add(cmbSem);
            Grid.SetColumn(semPanel, 2); row3.Children.Add(semPanel);
            sp.Children.Add(row3);

            // Row 4: Notes
            sp.Children.Add(MakeLabel("📝 Ghi chú:"));
            var tbNotes = new TextBox { Text = existing?.Notes ?? "", FontSize = 12, Padding = new Thickness(8, 6, 8, 6), AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 40, Margin = new Thickness(0, 0, 0, 14), BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)), BorderThickness = new Thickness(1) };
            sp.Children.Add(tbNotes);

            // Buttons
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnCancel = new Button { Content = "Hủy", Width = 90, Height = 36, FontSize = 13, Margin = new Thickness(0, 0, 8, 0), Cursor = Cursors.Hand };
            btnCancel.Click += (s, e2) => wnd.Close();

            var btnSave = new Button
            {
                Content = isNew ? "✅ Tạo lớp" : "✅ Lưu thay đổi",
                Width = 140, Height = 36, FontSize = 13,
                Background = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand
            };
            btnSave.Click += async (s, e2) =>
            {
                if (string.IsNullOrWhiteSpace(tbClass.Text))
                {
                    MessageBox.Show("Vui lòng nhập tên lớp!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var className = tbClass.Text.Trim();
                var invalidChars = System.IO.Path.GetInvalidFileNameChars();
                if (className.Any(c => invalidChars.Contains(c)))
                {
                    MessageBox.Show("Tên lớp chứa ký tự không hợp lệ! Vui lòng không nhập các ký tự đặc biệt như \\ / : * ? \" < > | để tránh lỗi ghi file.", 
                        "⚠️ Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    string grade = (cmbGrade.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "10";
                    string sem = (cmbSem.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "HK2";

                    if (isNew)
                    {
                        await Task.Run(() => app.ClassRoster.CreateRoster(
                            className, grade, tbSubject.Text.Trim(),
                            tbTeacher.Text.Trim(), tbYear.Text.Trim(), sem));
                    }
                    else
                    {
                        existing!.ClassName = className;
                        existing.GradeLevel = grade;
                        existing.Subject = tbSubject.Text.Trim();
                        existing.TeacherName = tbTeacher.Text.Trim();
                        existing.SchoolYear = tbYear.Text.Trim();
                        existing.Semester = sem;
                        existing.Notes = tbNotes.Text.Trim();
                        await Task.Run(() => app.ClassRoster.UpdateRoster(existing));
                    }
                    wnd.Close();
                    LoadRosters();
                    MessageBox.Show(isNew ? "✅ Đã tạo lớp mới!" : "✅ Đã cập nhật!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            btnPanel.Children.Add(btnCancel);
            btnPanel.Children.Add(btnSave);
            sp.Children.Add(btnPanel);

            wnd.Content = new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            wnd.ShowDialog();
        }

        private async void DeleteRoster_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.DataContext is not Data.ClassRoster roster)
                return;

            var r = MessageBox.Show($"Xóa lớp \"{roster.ClassName}\" ({roster.Subject})?\nDanh sách HS sẽ bị gỡ khỏi lớp (HS không bị xóa).",
                "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (r == MessageBoxResult.Yes)
            {
                var app = (QASmartTouch.App)Application.Current;
                var rosterId = roster.Id;
                await Task.Run(() => app.ClassRoster.DeleteRoster(rosterId));
                LoadRosters();
                MessageBox.Show("✅ Đã xóa lớp.", "Thành công");
            }
        }

        private void ActivateRoster_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.DataContext is not Data.ClassRoster roster)
                return;

            SelectClassFilterItem(roster.Id); // Tự động kích hoạt ClassFilter_Changed đồng bộ hoàn chỉnh
            MessageBox.Show($"✅ Đã chọn lớp: {roster.DisplayName}\n{roster.StudentCount} học sinh đã được tải.",
                "Chọn lớp thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void ImportRosterCsv_Click(object sender, RoutedEventArgs e)
        {
            // Chọn hoặc tạo roster trước
            var app = (QASmartTouch.App)Application.Current;
            var rosters = app.ClassRoster.GetAllRosters();

            if (!rosters.Any())
            {
                MessageBox.Show("Vui lòng tạo lớp trước khi import!", "Chưa có lớp", MessageBoxButton.OK, MessageBoxImage.Information);
                CreateRoster_Click(sender, e);
                rosters = app.ClassRoster.GetAllRosters();
                if (!rosters.Any()) return;
            }

            // Chọn roster
            var selectWnd = new Window
            {
                Title = "📥 Chọn lớp để import", Width = 350, Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this), ResizeMode = ResizeMode.NoResize,
                Background = Brushes.White
            };
            var selectSp = new StackPanel { Margin = new Thickness(16) };
            selectSp.Children.Add(new TextBlock { Text = "Chọn lớp để import danh sách HS:", FontSize = 13, Margin = new Thickness(0, 0, 0, 8) });

            var lstRosters = new ListBox { Height = 120, FontSize = 12 };
            foreach (var r in rosters)
                lstRosters.Items.Add(new ListBoxItem { Content = $"{r.ClassName} - {r.Subject} (GV {r.TeacherName})", Tag = r.Id });
            lstRosters.SelectedIndex = 0;
            selectSp.Children.Add(lstRosters);

            var selectBtn = new Button
            {
                Content = "📥 Tiếp tục", Width = 120, Height = 34, FontSize = 13, Margin = new Thickness(0, 10, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            int selectedRosterId = 0;
            selectBtn.Click += (s, e2) =>
            {
                if (lstRosters.SelectedItem is ListBoxItem item)
                    selectedRosterId = (int)item.Tag;
                selectWnd.Close();
            };
            selectSp.Children.Add(selectBtn);
            selectWnd.Content = selectSp;
            selectWnd.ShowDialog();

            if (selectedRosterId == 0) return;

            // Chọn file CSV
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Nhập danh sách học sinh (CSV)",
                Filter = "CSV Files|*.csv|All Files|*.*"
            };
            if (dlg.ShowDialog() != true) return;

            var rosterId = selectedRosterId;
            var fileName = dlg.FileName;
            var (imported, skipped, message) = await Task.Run(() => app.ClassRoster.ImportStudentsFromCsv(rosterId, fileName));
            LoadRosters();
            LoadStudents();
            MessageBox.Show($"📥 {message}", "Kết quả Import", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void ExportRoster_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.DataContext is not Data.ClassRoster roster)
                return;

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = $"Xuất danh sách — {roster.ClassName}",
                Filter = "CSV|*.csv",
                FileName = $"DS_{roster.ClassName}_{DateTime.Now:yyyyMMdd}.csv"
            };
            if (dlg.ShowDialog() == true)
            {
                var filePath = dlg.FileName;
                var rosterId = roster.Id;
                var app = (QASmartTouch.App)Application.Current;
                var success = await Task.Run(() => app.ClassRoster.ExportRosterToCsv(rosterId, filePath));
                if (success)
                    MessageBox.Show($"✅ Đã xuất danh sách {roster.ClassName}!", "Export thành công");
                else
                    MessageBox.Show("Lỗi khi xuất file.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  STUDENT AVATAR MANAGEMENT
        // ═══════════════════════════════════════════════════════════

        /// <summary>Click button to change student avatar</summary>
        private async void ChangeStudentAvatar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not FrameworkElement fe || fe.DataContext is not Data.Student student)
                    return;

                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Title = $"Chọn ảnh đại diện — {student.FullName}",
                    Filter = "Hình ảnh|*.jpg;*.jpeg;*.png;*.bmp;*.webp|Tất cả|*.*",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                };

                if (dlg.ShowDialog() == true)
                {
                    var sourcePath = dlg.FileName;
                    var studentId = student.Id;
                    var app = (QASmartTouch.App)Application.Current;

                    await Task.Run(() =>
                    {
                        var avatarDir = Path.Combine(
                            QASmartClass.Services.AppPaths.RootDir, "Avatars", "Students");
                        Directory.CreateDirectory(avatarDir);

                        var ext = Path.GetExtension(sourcePath);
                        var targetPath = Path.Combine(avatarDir, $"student_{studentId}{ext}");
                        File.Copy(sourcePath, targetPath, true);

                        using (var db = new AppDbContext())
                        {
                            var dbStudent = db.Students.Find(studentId);
                            if (dbStudent != null)
                            {
                                dbStudent.AvatarPath = targetPath;
                                db.SaveChanges();
                            }
                        }
                    });

                    LoadStudents();
                    MessageBox.Show($"Đã cập nhật ảnh đại diện cho {student.FullName}",
                        "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Student avatar error: {Err}", ex.Message);
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  REMEMBERED DEVICES MANAGEMENT
        // ═══════════════════════════════════════════════════════════

        private async void LoadDevices()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var result = await Task.Run(() =>
                {
                    return (
                        Devices: app.DeviceMemory.GetAllDevices(),
                        Connected: app.NetworkService.GetConnectedStudents()
                    );
                });

                var devices = result.Devices ?? new List<RememberedDevice>();
                var connected = result.Connected;

                deviceDataGrid.ItemsSource = devices;
                txtDeviceCount.Text = $"{devices.Count} thiết bị";

                if (devices.Count > 0)
                {
                    var connectedPCs = connected.Select(c => c.PCName).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    int offlineCount = devices.Count(d => !connectedPCs.Contains(d.PCName));

                    txtDeviceStatus.Text = $"💾 {devices.Count} thiết bị đã ghi nhớ | " +
                        $"🟢 {devices.Count - offlineCount} đang kết nối | " +
                        $"⚫ {offlineCount} chưa kết nối | " +
                        $"📊 Tổng {devices.Sum(d => d.ConnectionCount)} lượt kết nối";
                }
                else
                {
                    txtDeviceStatus.Text = "Thiết bị sẽ được tự động ghi nhớ khi học sinh kết nối vào lớp.";
                }

                Log.Information("Loaded {Count} remembered devices", devices.Count);
            }
            catch (Exception ex)
            {
                Log.Warning("LoadDevices error: {Err}", ex.Message);
            }
        }

        private void DeviceExpander_Expanded(object sender, RoutedEventArgs e)
        {
            LoadDevices();
        }

        private void RefreshDevices_Click(object sender, RoutedEventArgs e)
        {
            LoadDevices();
        }

        private async void ClearAllDevices_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Xóa tất cả thiết bị đã ghi nhớ?\nCác thiết bị sẽ được ghi nhớ lại khi kết nối lần sau.",
                "⚠️ Xác nhận xóa tất cả",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    int count = 0;
                    await Task.Run(() => { count = app.DeviceMemory.ClearAll(); });
                    LoadDevices();
                    MessageBox.Show($"✅ Đã xóa {count} thiết bị.", "Thành công");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditDeviceNote_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.DataContext is not Data.RememberedDevice device)
                return;

            // Simple input dialog for notes
            var wnd = new Window
            {
                Title = $"📝 Ghi chú — {device.PCName}",
                Width = 400, Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this), ResizeMode = ResizeMode.NoResize,
                Background = Brushes.White
            };

            var sp = new StackPanel { Margin = new Thickness(16) };
            sp.Children.Add(new TextBlock
            {
                Text = $"Ghi chú cho máy: {device.PCName} ({device.StudentName})",
                FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                Margin = new Thickness(0, 0, 0, 8)
            });

            var tbNote = new TextBox
            {
                Text = device.Notes, FontSize = 13, AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap, MinHeight = 60,
                Padding = new Thickness(8, 6, 8, 6),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)), BorderThickness = new Thickness(1)
            };
            sp.Children.Add(tbNote);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            var btnCancel = new Button { Content = "Hủy", Width = 80, Height = 32, Margin = new Thickness(0, 0, 8, 0), Cursor = Cursors.Hand };
            btnCancel.Click += (s, e2) => wnd.Close();

            var btnSave = new Button
            {
                Content = "✅ Lưu", Width = 100, Height = 32,
                Background = new SolidColorBrush(Color.FromRgb(0, 105, 92)),
                Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand
            };
            btnSave.Click += async (s, e2) =>
            {
                var app = (QASmartTouch.App)Application.Current;
                var noteText = tbNote.Text.Trim();
                await Task.Run(() => app.DeviceMemory.UpdateNotes(device.Id, noteText));
                wnd.Close();
                LoadDevices();
            };

            btnPanel.Children.Add(btnCancel);
            btnPanel.Children.Add(btnSave);
            sp.Children.Add(btnPanel);

            wnd.Content = sp;
            wnd.ShowDialog();
        }

        private async void RemoveDevice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.DataContext is not Data.RememberedDevice device)
                return;

            var result = MessageBox.Show(
                $"Xóa thiết bị \"{device.PCName}\" ({device.StudentName})?\nThiết bị sẽ được ghi nhớ lại khi kết nối lần sau.",
                "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var app = (QASmartTouch.App)Application.Current;
                var deviceId = device.Id;
                await Task.Run(() => app.DeviceMemory.RemoveDevice(deviceId));
                LoadDevices();
            }
        }

        private void DeviceGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (deviceDataGrid.SelectedItem is Data.RememberedDevice device)
            {
                var info = $"💻 Máy tính: {device.PCName}\n" +
                           $"🌐 IP gần nhất: {device.LastKnownIP}\n" +
                           $"👤 Học sinh: {device.StudentName} ({device.StudentCode})\n" +
                           $"📱 Phiên bản: {device.ClientVersion}\n" +
                           $"🔗 Số lần kết nối: {device.ConnectionCount}\n" +
                           $"📅 Lần đầu: {device.FirstSeen:HH:mm dd/MM/yyyy}\n" +
                           $"⏰ Lần cuối: {device.LastConnected:HH:mm dd/MM/yyyy}\n" +
                           $"📝 Ghi chú: {(string.IsNullOrEmpty(device.Notes) ? "(không)" : device.Notes)}";

                MessageBox.Show(info, $"Thông tin thiết bị — {device.PCName}",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnListView_Click(object sender, RoutedEventArgs e)
        {
            btnListView.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1976D2"));
            btnListView.Foreground = Brushes.White;
            btnListView.BorderThickness = new Thickness(0);

            btnSeatingView.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"));
            btnSeatingView.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#37474F"));
            btnSeatingView.BorderThickness = new Thickness(1);

            borderListView.Visibility = Visibility.Visible;
            borderSeatingChartView.Visibility = Visibility.Collapsed;
            borderAssetChartView.Visibility = Visibility.Collapsed;
            btnAssetView.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"));
            btnAssetView.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#37474F"));
            btnAssetView.BorderThickness = new Thickness(1);
        }

        private void BtnSeatingView_Click(object sender, RoutedEventArgs e)
        {
            btnSeatingView.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1976D2"));
            btnSeatingView.Foreground = Brushes.White;
            btnSeatingView.BorderThickness = new Thickness(0);

            btnListView.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"));
            btnListView.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#37474F"));
            btnListView.BorderThickness = new Thickness(1);

            borderListView.Visibility = Visibility.Collapsed;
            borderSeatingChartView.Visibility = Visibility.Visible;

            RenderSeatingChart();
        }

        private Border? _draggedElement;
        private Point _dragStartPoint;
        private double _originalLeft;
        private double _originalTop;
        private System.Windows.Threading.DispatcherTimer? _longPressTimer;
        private Border? _pendingDragElement;
        private Point _pendingStartPoint;
        private bool _isLongPressTriggered = false;

        private void RenderSeatingChart()
        {
            if (SeatingCanvas == null) return;
            SeatingCanvas.Children.Clear();

            // Set background grid DrawingBrush
            var gridBrush = new DrawingBrush
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, 40, 40),
                ViewportUnits = BrushMappingMode.Absolute
            };
            var geometryDrawing = new GeometryDrawing
            {
                Pen = new Pen(new SolidColorBrush(Color.FromRgb(226, 232, 240)), 0.5)
            };
            var geometryGroup = new GeometryGroup();
            geometryGroup.Children.Add(new LineGeometry(new Point(0, 0), new Point(40, 0)));
            geometryGroup.Children.Add(new LineGeometry(new Point(0, 0), new Point(0, 40)));
            geometryDrawing.Geometry = geometryGroup;
            gridBrush.Drawing = geometryDrawing;
            SeatingCanvas.Background = gridBrush;

            var app = (QASmartTouch.App)Application.Current;


            double canvasWidth = SeatingCanvas.ActualWidth > 0 ? SeatingCanvas.ActualWidth : 1000;
            double canvasHeight = SeatingCanvas.ActualHeight > 0 ? SeatingCanvas.ActualHeight : 600;
            bool isEmergency = btnEmergencyMode?.IsChecked == true;
            var exits = new List<Point>
            {
                new Point(canvasWidth - 80, 40),
                new Point(60, canvasHeight - 50)
            };
            if (isEmergency)
            {
                foreach (var exit in exits)
                {
                    var exitBorder = new Border
                    {
                        Width = 100,
                        Height = 40,
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9")),
                        BorderBrush = Brushes.Green,
                        BorderThickness = new Thickness(2),
                        CornerRadius = new CornerRadius(4),
                        Child = new TextBlock
                        {
                            Text = "\uD83D\uDEAA L\u1ED4I THO\u00C1T",
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.Green,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontSize = 11
                        }
                    };
                    var glowAnim = new System.Windows.Media.Animation.DoubleAnimation
                    {
                        From = 1.0,
                        To = 0.3,
                        Duration = TimeSpan.FromSeconds(0.6),
                        AutoReverse = true,
                        RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
                    };
                    exitBorder.BeginAnimation(UIElement.OpacityProperty, glowAnim);
                    Canvas.SetLeft(exitBorder, exit.X - 50);
                    Canvas.SetTop(exitBorder, exit.Y - 20);
                    SeatingCanvas.Children.Add(exitBorder);
                }
                foreach (var student in _allStudents)
                {
                    double sLeft = student.PositionX * canvasWidth;
                    double sTop = student.PositionY * canvasHeight;
                    double cardCenterX = sLeft + 60;
                    double cardCenterY = sTop + 35;
                    Point closestExit = exits[0];
                    double minDist = double.MaxValue;
                    foreach (var exit in exits)
                    {
                        double dist = Math.Pow(cardCenterX - exit.X, 2) + Math.Pow(cardCenterY - exit.Y, 2);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            closestExit = exit;
                        }
                    }
                    var flowLine = new System.Windows.Shapes.Line
                    {
                        X1 = cardCenterX,
                        Y1 = cardCenterY,
                        X2 = closestExit.X,
                        Y2 = closestExit.Y,
                        Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                        StrokeThickness = 3,
                        StrokeDashArray = new DoubleCollection { 6, 6 },
                        Opacity = 0.75
                    };
                    var dashAnim = new System.Windows.Media.Animation.DoubleAnimation
                    {
                        From = 20.0,
                        To = 0.0,
                        Duration = TimeSpan.FromSeconds(1.2),
                        RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
                    };
                    flowLine.BeginAnimation(System.Windows.Shapes.Shape.StrokeDashOffsetProperty, dashAnim);
                    SeatingCanvas.Children.Add(flowLine);
                }
            }

            double itemWidth = 150;
            double itemHeight = 90;
            double spacingX = 30;
            double spacingY = 30;

            int cols = Math.Max(3, (int)Math.Floor((canvasWidth - 100) / (itemWidth + spacingX)));
            double startX = 50;
            double startY = 40;

            int numRows = (int)Math.Ceiling((double)_allStudents.Count / cols);
            double requiredHeight = startY + numRows * (itemHeight + spacingY) + 50;
            if (requiredHeight > canvasHeight)
            {
                SeatingCanvas.Height = requiredHeight;
                canvasHeight = requiredHeight;
            }
            else
            {
                SeatingCanvas.Height = double.NaN;
            }

            for (int i = 0; i < _allStudents.Count; i++)
            {
                var student = _allStudents[i];
                double left, top;

                if (student.PositionX > 0.001 || student.PositionY > 0.001)
                {
                    left = student.PositionX * canvasWidth;
                    top = student.PositionY * canvasHeight;
                }
                else
                {
                    int r = i / cols;
                    int c = i % cols;
                    left = startX + c * (itemWidth + spacingX);
                    top = startY + r * (itemHeight + spacingY);

                    // Save initial layout back to model
                    student.PositionX = left / canvasWidth;
                    student.PositionY = top / canvasHeight;
                }

                // Ensure boundaries
                left = Math.Max(0, Math.Min(canvasWidth - itemWidth, left));
                top = Math.Max(0, Math.Min(canvasHeight - itemHeight, top));

                // Create Card element
                var card = new Border
                {
                    Width = itemWidth,
                    Height = itemHeight,
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(student.IsOnline ? "#4CAF50" : "#BDBDBD")),
                    BorderThickness = new Thickness(student.IsOnline ? 2 : 1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10),
                    Tag = student,
                    Cursor = Cursors.SizeAll
                };

                var toolTipContent = $"Học sinh: {student.FullName}\nMã HS: {student.StudentCode}\nMáy: {student.PCName}\nIP: {student.IPAddress}";
                card.ToolTip = toolTipContent;
                System.Windows.Controls.ToolTipService.SetInitialShowDelay(card, 0);

                // Add drop shadow
                var shadow = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 5,
                    ShadowDepth = 1,
                    Opacity = 0.1
                };
                card.Effect = shadow;

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Row 0: Status Ellipse + Full Name
                var headerStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
                var statusEllipse = new System.Windows.Shapes.Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = student.IsOnline ? Brushes.Green : Brushes.Gray,
                    Margin = new Thickness(0, 0, 6, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                var nameText = new TextBlock
                {
                    Text = student.FullName,
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#212121")),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = 110,
                    VerticalAlignment = VerticalAlignment.Center
                };
                headerStack.Children.Add(statusEllipse);
                headerStack.Children.Add(nameText);
                Grid.SetRow(headerStack, 0);
                grid.Children.Add(headerStack);

                // Row 1: PC Name
                var infoText = new TextBlock
                {
                    Text = string.IsNullOrEmpty(student.PCName) ? "PC: N/A" : $"Máy: {student.PCName}",
                    FontSize = 11,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#546E7A")),
                    Margin = new Thickness(0, 0, 0, 2)
                };
                Grid.SetRow(infoText, 1);
                grid.Children.Add(infoText);

                // Row 2: IP Address
                var ipText = new TextBlock
                {
                    Text = string.IsNullOrEmpty(student.IPAddress) ? "IP: N/A" : student.IPAddress,
                    FontSize = 10,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#78909C"))
                };
                Grid.SetRow(ipText, 2);
                grid.Children.Add(ipText);

                var loadingRing = new System.Windows.Shapes.Ellipse
                {
                    Width = 18,
                    Height = 18,
                    Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")),
                    StrokeThickness = 2.5,
                    StrokeDashArray = new DoubleCollection(new double[] { 4, 2 }),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Visibility = Visibility.Collapsed,
                    Margin = new Thickness(0, 2, 2, 0)
                };
                var rotateTransform = new RotateTransform();
                loadingRing.RenderTransform = rotateTransform;
                loadingRing.RenderTransformOrigin = new Point(0.5, 0.5);
                var rotateAnim = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0,
                    To = 360,
                    Duration = TimeSpan.FromSeconds(1),
                    RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
                };
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);
                Grid.SetRow(loadingRing, 0);
                grid.Children.Add(loadingRing);

                // Row 2 Device Indicators (Bottom-Right)
                var deviceStack = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 2, 0)
                };

                var connStudent = app.ClassroomSession?.ConnectedStudents?.FirstOrDefault(s => s.StudentCode.Equals(student.StudentCode, StringComparison.OrdinalIgnoreCase));
                bool isHeadphoneOk = connStudent?.IsHeadphoneOk ?? true;
                bool isMicOk = connStudent?.IsMicOk ?? true;

                if (student.IsOnline)
                {
                    var hpText = new TextBlock
                    {
                        Text = "🎧",
                        FontSize = 12,
                        Foreground = isHeadphoneOk ? Brushes.Green : Brushes.Red,
                        Margin = new Thickness(0, 0, 6, 0),
                        ToolTip = isHeadphoneOk ? "Tai nghe: Đang kết nối" : "Tai nghe: Rút dây / Lỗi"
                    };

                    var micText = new TextBlock
                    {
                        Text = "🎙️",
                        FontSize = 12,
                        Foreground = isMicOk ? Brushes.Green : Brushes.Red,
                        ToolTip = isMicOk ? "Microphone: Hoạt động" : "Microphone: Lỗi / Tắt tiếng"
                    };

                    if (!isHeadphoneOk || !isMicOk)
                    {
                        var blinkAnim = new System.Windows.Media.Animation.DoubleAnimation
                        {
                            From = 1.0,
                            To = 0.2,
                            Duration = TimeSpan.FromSeconds(0.8),
                            AutoReverse = true,
                            RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
                        };
                        if (!isHeadphoneOk) hpText.BeginAnimation(UIElement.OpacityProperty, blinkAnim);
                        if (!isMicOk) micText.BeginAnimation(UIElement.OpacityProperty, blinkAnim);
                    }

                    deviceStack.Children.Add(hpText);
                    deviceStack.Children.Add(micText);
                }
                Grid.SetRow(deviceStack, 2);
                grid.Children.Add(deviceStack);

                card.Child = grid;

                // Mouse/Touch Drag Events - Đòi hỏi nhấn giữ 0.8s
                card.PreviewMouseLeftButtonDown += (s, e) =>
                {
                    if (_longPressTimer != null)
                    {
                        _longPressTimer.Stop();
                        _longPressTimer = null;
                    }

                    _pendingDragElement = card;
                    _pendingStartPoint = e.GetPosition(SeatingCanvas);
                    _isLongPressTriggered = false;
                    loadingRing.Visibility = Visibility.Visible;

                    _longPressTimer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(800)
                    };
                    _longPressTimer.Tick += (timerSender, timerArgs) =>
                    {
                        _longPressTimer?.Stop();
                        _longPressTimer = null;

                        if (_pendingDragElement == card)
                        {
                            _isLongPressTriggered = true;
                            _draggedElement = card;
                            _dragStartPoint = _pendingStartPoint;
                            _originalLeft = Canvas.GetLeft(card);
                            _originalTop = Canvas.GetTop(card);

                            loadingRing.Visibility = Visibility.Collapsed;

                            // Hiệu ứng rung nhẹ báo hiệu kéo được
                            var transform = new TranslateTransform();
                            card.RenderTransform = transform;
                            var shakeAnim = new System.Windows.Media.Animation.DoubleAnimation
                            {
                                From = -2,
                                To = 2,
                                Duration = TimeSpan.FromMilliseconds(50),
                                AutoReverse = true,
                                RepeatBehavior = new System.Windows.Media.Animation.RepeatBehavior(4)
                            };
                            transform.BeginAnimation(TranslateTransform.XProperty, shakeAnim);

                            // Hiển thị viền xanh dương
                            card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
                            card.BorderThickness = new Thickness(3);

                            card.CaptureMouse();
                        }
                    };
                    _longPressTimer.Start();
                    e.Handled = true;
                };

                card.MouseMove += (s, e) =>
                {
                    if (_pendingDragElement == card && !_isLongPressTriggered)
                    {
                        var currentPoint = e.GetPosition(SeatingCanvas);
                        double dist = Math.Sqrt(Math.Pow(currentPoint.X - _pendingStartPoint.X, 2) + Math.Pow(currentPoint.Y - _pendingStartPoint.Y, 2));
                        if (dist > 8.0) // Di chuyển quá 8px -> Huỷ
                        {
                            if (_longPressTimer != null)
                            {
                                _longPressTimer.Stop();
                                _longPressTimer = null;
                            }
                            _pendingDragElement = null;
                            loadingRing.Visibility = Visibility.Collapsed;
                        }
                    }
                };

                card.PreviewMouseLeftButtonUp += (s, e) =>
                {
                    if (_longPressTimer != null)
                    {
                        _longPressTimer.Stop();
                        _longPressTimer = null;
                    }
                    _pendingDragElement = null;
                    loadingRing.Visibility = Visibility.Collapsed;

                    if (!_isLongPressTriggered)
                    {
                        card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(student.IsOnline ? "#4CAF50" : "#BDBDBD"));
                        card.BorderThickness = new Thickness(student.IsOnline ? 2 : 1);
                    }
                };

                // Hỗ trợ sự kiện cảm ứng (Touch)
                card.TouchDown += (s, e) =>
                {
                    if (_longPressTimer != null)
                    {
                        _longPressTimer.Stop();
                        _longPressTimer = null;
                    }

                    _pendingDragElement = card;
                    _pendingStartPoint = e.GetTouchPoint(SeatingCanvas).Position;
                    _isLongPressTriggered = false;
                    loadingRing.Visibility = Visibility.Visible;

                    _longPressTimer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(800)
                    };
                    _longPressTimer.Tick += (timerSender, timerArgs) =>
                    {
                        _longPressTimer?.Stop();
                        _longPressTimer = null;

                        if (_pendingDragElement == card)
                        {
                            _isLongPressTriggered = true;
                            _draggedElement = card;
                            _dragStartPoint = _pendingStartPoint;
                            _originalLeft = Canvas.GetLeft(card);
                            _originalTop = Canvas.GetTop(card);

                            loadingRing.Visibility = Visibility.Collapsed;

                            var transform = new TranslateTransform();
                            card.RenderTransform = transform;
                            var shakeAnim = new System.Windows.Media.Animation.DoubleAnimation
                            {
                                From = -2,
                                To = 2,
                                Duration = TimeSpan.FromMilliseconds(50),
                                AutoReverse = true,
                                RepeatBehavior = new System.Windows.Media.Animation.RepeatBehavior(4)
                            };
                            transform.BeginAnimation(TranslateTransform.XProperty, shakeAnim);

                            card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
                            card.BorderThickness = new Thickness(3);

                            card.CaptureTouch(e.TouchDevice);
                        }
                    };
                    _longPressTimer.Start();
                    e.Handled = true;
                };

                card.TouchMove += (s, e) =>
                {
                    if (_pendingDragElement == card && !_isLongPressTriggered)
                    {
                        var currentPoint = e.GetTouchPoint(SeatingCanvas).Position;
                        double dist = Math.Sqrt(Math.Pow(currentPoint.X - _pendingStartPoint.X, 2) + Math.Pow(currentPoint.Y - _pendingStartPoint.Y, 2));
                        if (dist > 20.0)
                        {
                            if (_longPressTimer != null)
                            {
                                _longPressTimer.Stop();
                                _longPressTimer = null;
                            }
                            _pendingDragElement = null;
                            loadingRing.Visibility = Visibility.Collapsed;
                        }
                    }
                };

                card.TouchUp += (s, e) =>
                {
                    if (_longPressTimer != null)
                    {
                        _longPressTimer.Stop();
                        _longPressTimer = null;
                    }
                    _pendingDragElement = null;
                    loadingRing.Visibility = Visibility.Collapsed;

                    if (!_isLongPressTriggered)
                    {
                        card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(student.IsOnline ? "#4CAF50" : "#BDBDBD"));
                        card.BorderThickness = new Thickness(student.IsOnline ? 2 : 1);
                    }
                };

                Canvas.SetLeft(card, left);
                Canvas.SetTop(card, top);
                SeatingCanvas.Children.Add(card);
            }
        }

                private void SeatingCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedElement != null)
            {
                var currentPoint = e.GetPosition(SeatingCanvas);
                double deltaX = currentPoint.X - _dragStartPoint.X;
                double deltaY = currentPoint.Y - _dragStartPoint.Y;
                double newLeft = _originalLeft + deltaX;
                double newTop = _originalTop + deltaY;
                // Bound check
                newLeft = Math.Max(0, Math.Min(SeatingCanvas.ActualWidth - _draggedElement.Width, newLeft));
                newTop = Math.Max(0, Math.Min(SeatingCanvas.ActualHeight - _draggedElement.Height, newTop));
                // Apply Snap-to-Grid if enabled
                if (chkSnapToGrid.IsChecked == true)
                {
                    newLeft = Math.Round(newLeft / 20.0) * 20.0;
                    newTop = Math.Round(newTop / 20.0) * 20.0;
                }
                Canvas.SetLeft(_draggedElement, newLeft);
                Canvas.SetTop(_draggedElement, newTop);
            }
        }

                private async void SeatingCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggedElement != null)
            {
                _draggedElement.ReleaseMouseCapture();
                var student = _draggedElement.Tag as Student;
                if (student != null)
                {
                    double left = Canvas.GetLeft(_draggedElement);
                    double top = Canvas.GetTop(_draggedElement);
                    var app = (QASmartTouch.App)Application.Current;
                    double canvasWidth = SeatingCanvas.ActualWidth > 0 ? SeatingCanvas.ActualWidth : 1000;
                    double canvasHeight = SeatingCanvas.ActualHeight > 0 ? SeatingCanvas.ActualHeight : 600;
                    student.PositionX = left / canvasWidth;
                    student.PositionY = top / canvasHeight;
                    try
                    {
                        var dbStudent = app.Database.Students.FirstOrDefault(s => s.Id == student.Id);
                        if (dbStudent != null)
                        {
                            dbStudent.PositionX = student.PositionX;
                            dbStudent.PositionY = student.PositionY;
                            await app.Database.SaveChangesAsync();
                            Log.Information("Saved seating chart coordinates for student {Name} ({X}, {Y})", student.FullName, student.PositionX, student.PositionY);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to save seating chart coordinates");
                    }
                }
                if (student != null)
                {
                    _draggedElement.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(student.IsOnline ? "#4CAF50" : "#BDBDBD"));
                    _draggedElement.BorderThickness = new Thickness(student.IsOnline ? 2 : 1);
                }
                _draggedElement = null;
            }
        }

        private void SeatingCanvas_TouchMove(object sender, TouchEventArgs e)
        {
            if (_draggedElement != null && _isLongPressTriggered)
            {
                var currentPoint = e.GetTouchPoint(SeatingCanvas).Position;
                double deltaX = currentPoint.X - _dragStartPoint.X;
                double deltaY = currentPoint.Y - _dragStartPoint.Y;
                double newLeft = _originalLeft + deltaX;
                double newTop = _originalTop + deltaY;
                // Bound check
                newLeft = Math.Max(0, Math.Min(SeatingCanvas.ActualWidth - _draggedElement.Width, newLeft));
                newTop = Math.Max(0, Math.Min(SeatingCanvas.ActualHeight - _draggedElement.Height, newTop));
                // Apply Snap-to-Grid if enabled
                if (chkSnapToGrid.IsChecked == true)
                {
                    newLeft = Math.Round(newLeft / 20.0) * 20.0;
                    newTop = Math.Round(newTop / 20.0) * 20.0;
                }
                Canvas.SetLeft(_draggedElement, newLeft);
                Canvas.SetTop(_draggedElement, newTop);
                e.Handled = true;
            }
        }

        private async void SeatingCanvas_TouchUp(object sender, TouchEventArgs e)
        {
            if (_draggedElement != null)
            {
                _draggedElement.ReleaseTouchCapture(e.TouchDevice);
                var student = _draggedElement.Tag as Student;
                if (student != null)
                {
                    double left = Canvas.GetLeft(_draggedElement);
                    double top = Canvas.GetTop(_draggedElement);
                    var app = (QASmartTouch.App)Application.Current;
                    double canvasWidth = SeatingCanvas.ActualWidth > 0 ? SeatingCanvas.ActualWidth : 1000;
                    double canvasHeight = SeatingCanvas.ActualHeight > 0 ? SeatingCanvas.ActualHeight : 600;
                    student.PositionX = left / canvasWidth;
                    student.PositionY = top / canvasHeight;
                    try
                    {
                        var dbStudent = app.Database.Students.FirstOrDefault(s => s.Id == student.Id);
                        if (dbStudent != null)
                        {
                            dbStudent.PositionX = student.PositionX;
                            dbStudent.PositionY = student.PositionY;
                            await app.Database.SaveChangesAsync();
                            Log.Information("Saved seating chart touch coordinates for student {Name} ({X}, {Y})", student.FullName, student.PositionX, student.PositionY);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to save seating chart touch coordinates");
                    }
                }
                if (student != null)
                {
                    _draggedElement.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(student.IsOnline ? "#4CAF50" : "#BDBDBD"));
                    _draggedElement.BorderThickness = new Thickness(student.IsOnline ? 2 : 1);
                }
                _draggedElement = null;
            }
        }

        private string ComputeHmacSha256(string message, string secret)
        {
            var encoding = new System.Text.UTF8Encoding();
            byte[] keyByte = encoding.GetBytes(secret);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacsha256 = new System.Security.Cryptography.HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                return System.Convert.ToHexString(hashmessage);
            }
        }

        private void SeatingCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RenderSeatingChart();
        }

        private async void BtnResetSeatingLayout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Bạn có muốn sắp xếp lại toàn bộ sơ đồ lớp học về dạng lưới mặc định?", "Xác nhận sắp xếp", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    foreach (var student in _allStudents)
                    {
                        student.PositionX = 0.0;
                        student.PositionY = 0.0;

                        var dbStudent = app.Database.Students.FirstOrDefault(s => s.Id == student.Id);
                        if (dbStudent != null)
                        {
                            dbStudent.PositionX = 0.0;
                            dbStudent.PositionY = 0.0;
                        }
                    }
                    await app.Database.SaveChangesAsync();
                    RenderSeatingChart();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to reset seating layout");
                }
            }
        }

        private void NetworkService_MessageReceived(object? sender, StudentMessageEventArgs e)
        {
            if (e.Message.StartsWith("CLAIM_SUCCESS") || e.Message.StartsWith("PERIPHERAL_UPDATE"))
            {
                Dispatcher.Invoke(() =>
                {
                    LoadStudents();
                });
            }
        }

        private void NetworkService_StudentConnected(object? sender, StudentConnectedEventArgs e)
        {
            Dispatcher.Invoke(() => { LoadStudents(); });
        }

        private void NetworkService_StudentDisconnected(object? sender, string e)
        {
            Dispatcher.Invoke(() => { LoadStudents(); });
        }
        private void BtnAssetView_Click(object sender, RoutedEventArgs e)
        {
            btnListView.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"));
            btnListView.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#37474F"));
            btnListView.BorderThickness = new Thickness(1);

            btnSeatingView.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"));
            btnSeatingView.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#37474F"));
            btnSeatingView.BorderThickness = new Thickness(1);

            btnAssetView.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1976D2"));
            btnAssetView.Foreground = Brushes.White;
            btnAssetView.BorderThickness = new Thickness(0);

            borderListView.Visibility = Visibility.Collapsed;
            borderSeatingChartView.Visibility = Visibility.Collapsed;
            borderAssetChartView.Visibility = Visibility.Visible;

            RenderAssetLayout();
        }

        private Border? _draggedAsset;

        private void RenderAssetLayout()
        {
            if (AssetCanvas == null) return;
            AssetCanvas.Children.Clear();

            var app = (QASmartTouch.App)Application.Current;
            var assets = app.Database.SchoolAssets.ToList();

            if (!assets.Any())
            {
                app.Database.SchoolAssets.Add(new SchoolAsset { AssetType = "Smartboard", AssetCode = "SB-001", Location = "Phòng Lab", PositionX = 0.5, PositionY = 0.1 });
                app.Database.SchoolAssets.Add(new SchoolAsset { AssetType = "Projector", AssetCode = "PRJ-001", Location = "Phòng Lab", PositionX = 0.15, PositionY = 0.15 });
                app.Database.SchoolAssets.Add(new SchoolAsset { AssetType = "Router", AssetCode = "RT-001", Location = "Phòng Lab", PositionX = 0.85, PositionY = 0.15 });
                app.Database.SaveChanges();
                assets = app.Database.SchoolAssets.ToList();
            }

            double canvasWidth = AssetCanvas.ActualWidth > 0 ? AssetCanvas.ActualWidth : 1000;
            double canvasHeight = AssetCanvas.ActualHeight > 0 ? AssetCanvas.ActualHeight : 600;

            foreach (var asset in assets)
            {
                double left = asset.PositionX * canvasWidth;
                double top = asset.PositionY * canvasHeight;

                if (left < 0 || left > canvasWidth - 140) left = 100;
                if (top < 0 || top > canvasHeight - 70) top = 100;

                var iconStr = asset.AssetType.ToLower() switch
                {
                    "smartboard" => "\uD83D\uDCFA B\u1EA3ng thông minh",
                    "projector" => "\uD83D\uDCFD Máy chi\u1EBFu",
                    "router" => "\uD83D\uDCF6 Wifi Router",
                    _ => "\uD83D\uDCE6 Thi\u1EBFt b\u1ECB"
                };

                var card = new Border
                {
                    Width = 140,
                    Height = 65,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0F7FA")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00838F")),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(8),
                    Tag = asset,
                    Cursor = Cursors.Hand
                };

                var stack = new StackPanel();
                stack.Children.Add(new TextBlock { Text = iconStr, FontWeight = FontWeights.Bold, FontSize = 12, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#006064")) });
                stack.Children.Add(new TextBlock { Text = $"Code: {asset.AssetCode}", FontSize = 10, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00838F")), Margin = new Thickness(0,2,0,0) });
                stack.Children.Add(new TextBlock { Text = $"Tr\u1EA1ng thái: {asset.Status}", FontSize = 9, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(asset.Status == "Good" ? "#2E7D32" : "#C62828")) });

                card.Child = stack;

                Canvas.SetLeft(card, left);
                Canvas.SetTop(card, top);

                card.MouseLeftButtonDown += (s, e) =>
                {
                    if (chkAssetEdit.IsChecked == true)
                    {
                        _draggedAsset = card;
                        _dragStartPoint = e.GetPosition(AssetCanvas);
                        _originalLeft = Canvas.GetLeft(card);
                        _originalTop = Canvas.GetTop(card);
                        card.CaptureMouse();
                    }
                };

                AssetCanvas.Children.Add(card);
            }
        }

        private void ChkAssetEdit_Click(object sender, RoutedEventArgs e)
        {
            if (chkAssetEdit.IsChecked == true)
            {
                MessageBox.Show("\uD83D\uDD27 \u0110\u00E3 b\u1EADt ch\u1EBF \u0111\u1ED9 thi\u1EBFt k\u1EBF. B\u1EA1n c\u00F3 th\u1EC3 k\u00E9o th\u1EA3 \u0111\u1EC3 s\u1EAFp x\u1EBFp v\u1ECB tr\u00ED c\u00E1c thi\u1EBFt b\u1ECB th\u01B0\u1EDDng tr\u1EF1c ph\u00F2ng Lab.", "Thi\u1EBFt k\u1EBF S\u01A1 \u0111\u1ED3 t\u00E0i s\u1EA3n", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AssetCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RenderAssetLayout();
        }

        private void AssetCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedAsset != null)
            {
                var currentPoint = e.GetPosition(AssetCanvas);
                double deltaX = currentPoint.X - _dragStartPoint.X;
                double deltaY = currentPoint.Y - _dragStartPoint.Y;

                double left = _originalLeft + deltaX;
                double top = _originalTop + deltaY;

                double canvasWidth = AssetCanvas.ActualWidth > 0 ? AssetCanvas.ActualWidth : 1000;
                double canvasHeight = AssetCanvas.ActualHeight > 0 ? AssetCanvas.ActualHeight : 600;

                left = Math.Max(0, Math.Min(canvasWidth - 140, left));
                top = Math.Max(0, Math.Min(canvasHeight - 65, top));

                Canvas.SetLeft(_draggedAsset, left);
                Canvas.SetTop(_draggedAsset, top);
            }
        }

        private async void AssetCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggedAsset != null)
            {
                _draggedAsset.ReleaseMouseCapture();
                var asset = _draggedAsset.Tag as SchoolAsset;
                if (asset != null)
                {
                    double left = Canvas.GetLeft(_draggedAsset);
                    double top = Canvas.GetTop(_draggedAsset);
                    double canvasWidth = AssetCanvas.ActualWidth > 0 ? AssetCanvas.ActualWidth : 1000;
                    double canvasHeight = AssetCanvas.ActualHeight > 0 ? AssetCanvas.ActualHeight : 600;

                    asset.PositionX = left / canvasWidth;
                    asset.PositionY = top / canvasHeight;
                    try
                    {
                        var app = (QASmartTouch.App)Application.Current;
                        var dbAsset = app.Database.SchoolAssets.FirstOrDefault(a => a.Id == asset.Id);
                        if (dbAsset != null)
                        {
                            dbAsset.PositionX = asset.PositionX;
                            dbAsset.PositionY = asset.PositionY;
                            await app.Database.SaveChangesAsync();
                            Log.Information("Saved asset coordinates for {Code} ({X}, {Y})", asset.AssetCode, asset.PositionX, asset.PositionY);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to save asset coordinates");
                    }
                }
                _draggedAsset = null;
            }
        }

        private void BtnEmergencyMode_Click(object sender, RoutedEventArgs e)
        {
            RenderSeatingChart();
        }
    }

    public class AvatarPathConverter : System.Windows.Data.IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string path && !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                try
                {
                    var bi = new System.Windows.Media.Imaging.BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; // Ngăn chặn khóa file
                    bi.UriSource = new Uri(path);
                    bi.EndInit();
                    return bi;
                }
                catch (Exception ex)
                {
                    Serilog.Log.Warning("AvatarPathConverter error: {Msg}", ex.Message);
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SeatNumberConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int num && num > 0)
                return num.ToString();
            return "--";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
