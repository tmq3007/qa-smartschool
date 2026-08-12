using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using QASmartClass.Data;
using QASmartClass.Classroom.Services;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class TeacherListPage : Page
    {
        private string _filter = "All";
        private string _searchText = "";

        public TeacherListPage()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadTeachers();
        }

        // ═══════════════════════════════════════════════════════════
        //  LOAD & DISPLAY
        // ═══════════════════════════════════════════════════════════

        private async void LoadTeachers()
        {
            if (listViewBody == null || txtTotalCount == null) return; // Not yet initialized
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                List<TeacherProfile> all = null;
                await Task.Run(() =>
                {
                    all = app.Database.TeacherProfiles.ToList();
                });

                // Stats
                txtTotalCount.Text = all.Count.ToString();
                txtActiveCount.Text = all.Count(t => t.IsActive).ToString();
                txtSubjectCount.Text = all.Where(t => !string.IsNullOrWhiteSpace(t.Subject))
                    .Select(t => t.Subject.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count().ToString();

                // Filter
                var filtered = _filter switch
                {
                    "Active" => all.Where(t => t.IsActive).ToList(),
                    "Inactive" => all.Where(t => !t.IsActive).ToList(),
                    _ => all
                };

                // Search
                if (!string.IsNullOrWhiteSpace(_searchText))
                {
                    var q = _searchText.ToLower();
                    filtered = filtered.Where(t =>
                        (t.TeacherCode?.ToLower().Contains(q) == true) ||
                        (t.FullName?.ToLower().Contains(q) == true) ||
                        (t.Subject?.ToLower().Contains(q) == true) ||
                        (t.Phone?.ToLower().Contains(q) == true) ||
                        (t.Email?.ToLower().Contains(q) == true) ||
                        (t.Notes?.ToLower().Contains(q) == true)).ToList();
                }

                txtResultCount.Text = $"Hiển thị {filtered.Count}/{all.Count} giáo viên";

                // Render
                listViewBody.Children.Clear();
                if (!filtered.Any())
                {
                    listViewBody.Children.Add(CreateEmptyState());
                    return;
                }

                int idx = 0;
                foreach (var t in filtered.OrderBy(x => x.FullName))
                {
                    idx++;
                    listViewBody.Children.Add(CreateRow(t, idx));
                }
            }
            catch (Exception ex)
            {
                Log.Warning("LoadTeachers error: {Err}", ex.Message);
            }
        }

        private Border CreateRow(TeacherProfile t, int index)
        {
            var row = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xF1, 0xF3, 0xF5)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(16, 10, 16, 10),
                Background = index % 2 == 0
                    ? new SolidColorBrush(Color.FromRgb(0xFB, 0xFC, 0xFD))
                    : Brushes.White
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });   // #
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });   // Mã GV
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Họ tên
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });   // Chức danh
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });  // Bộ môn
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });  // SĐT
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });  // Email
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });   // Status
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });  // Notes
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });  // Actions

            AddCell(grid, 0, index.ToString(), "#868E96", 11);
            AddCell(grid, 1, string.IsNullOrWhiteSpace(t.TeacherCode) ? "—" : t.TeacherCode, "#1565C0", 11, FontWeights.SemiBold);
            AddCell(grid, 2, t.FullName, "#212529", 12, FontWeights.SemiBold);
            AddCell(grid, 3, t.Title, "#495057", 11);
            AddCell(grid, 4, string.IsNullOrWhiteSpace(t.Subject) ? "—" : t.Subject, "#495057", 11);
            AddCell(grid, 5, string.IsNullOrWhiteSpace(t.Phone) ? "—" : t.Phone, "#495057", 11);
            AddCell(grid, 6, string.IsNullOrWhiteSpace(t.Email) ? "—" : t.Email, "#495057", 10);

            // Status badge
            var statusBorder = new Border
            {
                Background = t.IsActive
                    ? new SolidColorBrush(Color.FromRgb(0xE8, 0xF5, 0xE9))
                    : new SolidColorBrush(Color.FromRgb(0xFF, 0xEB, 0xEE)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 3, 8, 3),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            statusBorder.Child = new TextBlock
            {
                Text = t.IsActive ? "✅ HĐ" : "⏸ Nghỉ",
                FontSize = 10,
                Foreground = t.IsActive
                    ? new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32))
                    : new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)),
                FontFamily = new FontFamily("Segoe UI")
            };
            Grid.SetColumn(statusBorder, 7);
            grid.Children.Add(statusBorder);

            AddCell(grid, 8, string.IsNullOrWhiteSpace(t.Notes) ? "—" : t.Notes, "#868E96", 10);

            // Actions
            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            var btnEdit = MakeActionBtn("✏️", "#1976D2", "#E3F2FD");
            btnEdit.Tag = t.Id;
            btnEdit.Click += EditTeacher_Click;
            var btnDel = MakeActionBtn("🗑️", "#C62828", "#FFEBEE");
            btnDel.Tag = t.Id;
            btnDel.Click += DeleteTeacher_Click;
            actions.Children.Add(btnEdit);
            actions.Children.Add(btnDel);
            Grid.SetColumn(actions, 9);
            grid.Children.Add(actions);

            row.Child = grid;
            return row;
        }

        private static void AddCell(Grid g, int col, string text, string color, double size, FontWeight? weight = null)
        {
            double targetSize = size < 11 ? 11.5 : 12;
            var tb = new TextBlock
            {
                Text = text,
                FontSize = targetSize,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                FontFamily = new FontFamily("Segoe UI"),
                ToolTip = text
            };
            if (weight.HasValue) tb.FontWeight = weight.Value;
            Grid.SetColumn(tb, col);
            g.Children.Add(tb);
        }

        private static Button MakeActionBtn(string icon, string fg, string bg)
        {
            var btn = new Button
            {
                Content = icon,
                FontSize = 12,
                Width = 32, Height = 32,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg)),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fg)),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Margin = new Thickness(2, 0, 2, 0)
            };
            btn.Template = CreateRoundTemplate();
            return btn;
        }

        private static ControlTemplate CreateRoundTemplate()
        {
            var tmpl = new ControlTemplate(typeof(Button));
            var bd = new FrameworkElementFactory(typeof(Border), "bg");
            bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            bd.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            bd.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            bd.AppendChild(cp);
            tmpl.VisualTree = bd;
            var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(Border.OpacityProperty, 0.7, "bg"));
            tmpl.Triggers.Add(hover);
            return tmpl;
        }

        private static Border CreateEmptyState()
        {
            var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 60, 0, 60) };
            sp.Children.Add(new TextBlock { Text = "👨‍🏫", FontSize = 48, HorizontalAlignment = HorizontalAlignment.Center });
            sp.Children.Add(new TextBlock { Text = "Chưa có giáo viên nào", FontSize = 16, Foreground = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0), FontFamily = new FontFamily("Segoe UI") });
            sp.Children.Add(new TextBlock { Text = "Bấm \"➕ Thêm GV mới\" hoặc \"📥 Nhập CSV\" để bắt đầu", FontSize = 12, Foreground = Brushes.LightGray, HorizontalAlignment = HorizontalAlignment.Center, FontFamily = new FontFamily("Segoe UI") });
            return new Border { Child = sp };
        }

        // ═══════════════════════════════════════════════════════════
        //  ADD / EDIT DIALOG
        // ═══════════════════════════════════════════════════════════

        private void AddTeacher_Click(object sender, RoutedEventArgs e)
        {
            ShowTeacherDialog(null);
        }

        private void EditTeacher_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var app = (QASmartTouch.App)Application.Current;
                var teacher = app.Database.TeacherProfiles.FirstOrDefault(t => t.Id == id);
                if (teacher != null) ShowTeacherDialog(teacher);
            }
        }

        private void ShowTeacherDialog(TeacherProfile? existing)
        {
            bool isEdit = existing != null;
            var dlg = new Window
            {
                Title = isEdit ? "✏️ Sửa thông tin GV" : "➕ Thêm GV mới",
                Width = 480, Height = 560, ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this), Background = Brushes.White
            };

            var sp = new StackPanel { Margin = new Thickness(24) };

            // Title
            sp.Children.Add(new TextBlock { Text = isEdit ? "✏️ Chỉnh sửa Giáo viên" : "➕ Thêm Giáo viên", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0x21, 0x21, 0x21)), Margin = new Thickness(0, 0, 0, 16), FontFamily = new FontFamily("Segoe UI") });

            // Auto-generate TeacherCode for new teachers
            string autoCode = "";
            if (!isEdit)
            {
                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    int count = app.Database.TeacherProfiles.Count();
                    autoCode = $"GV{count + 1:D3}";
                    // Ensure uniqueness
                    while (app.Database.TeacherProfiles.Any(t => t.TeacherCode == autoCode))
                    {
                        count++;
                        autoCode = $"GV{count + 1:D3}";
                    }
                }
                catch { autoCode = $"GV{DateTime.Now:HHmmss}"; }
            }

            var txtCode = AddField(sp, "🔑 Mã GV (khóa) *", existing?.TeacherCode ?? autoCode);
            var txtName = AddField(sp, "👤 Họ tên *", existing?.FullName ?? "");
            var cboTitle = AddComboField(sp, "🎓 Chức danh", new[] { "GV", "ThS", "TS", "PGS", "GS" }, existing?.Title ?? "GV");
            var txtSubject = AddField(sp, "📚 Bộ môn", existing?.Subject ?? "");
            var txtSchool = AddField(sp, "🏫 Trường", existing?.School ?? "");
            var txtPhone = AddField(sp, "📱 SĐT", existing?.Phone ?? "");
            var txtEmail = AddField(sp, "📧 Email", existing?.Email ?? "");
            var txtNotes = AddField(sp, "📝 Ghi chú", existing?.Notes ?? "");
            var chkActive = new CheckBox { Content = " Đang hoạt động", IsChecked = existing?.IsActive ?? true, FontSize = 12, Margin = new Thickness(0, 8, 0, 0), FontFamily = new FontFamily("Segoe UI") };
            sp.Children.Add(chkActive);

            // Buttons
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
            var btnCancel = new Button { Content = "Hủy", Width = 90, Height = 36, FontSize = 12, Background = new SolidColorBrush(Color.FromRgb(0xF1, 0xF3, 0xF5)), Foreground = new SolidColorBrush(Color.FromRgb(0x49, 0x50, 0x57)), BorderThickness = new Thickness(0), Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 8, 0), IsCancel = true };
            btnCancel.Click += (s, e2) => dlg.Close();
            var btnSave = new Button { Content = isEdit ? "💾 Lưu" : "➕ Thêm", Width = 110, Height = 36, FontSize = 12, FontWeight = FontWeights.SemiBold, Background = new SolidColorBrush(Color.FromRgb(0x19, 0x76, 0xD2)), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand, IsDefault = true };
            btnSave.Click += async (s, e2) =>
            {
                var code = txtCode.Text.Trim();
                var name = txtName.Text.Trim();
                name = System.Text.RegularExpressions.Regex.Replace(name, @"\s+", " ");
                var phone = txtPhone.Text.Trim();
                var email = txtEmail.Text.Trim();

                if (string.IsNullOrEmpty(code))
                {
                    MessageBox.Show("Vui lòng nhập mã giáo viên!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Vui lòng nhập họ tên!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Xác thực định dạng SĐT & Email bằng Regex
                if (!string.IsNullOrEmpty(phone))
                {
                    var phoneRegex = new System.Text.RegularExpressions.Regex(@"^[0-9]{10,11}$");
                    if (!phoneRegex.IsMatch(phone))
                    {
                        MessageBox.Show("Số điện thoại không hợp lệ! Vui lòng chỉ nhập số (10 - 11 chữ số).", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                if (!string.IsNullOrEmpty(email))
                {
                    var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                    if (!emailRegex.IsMatch(email))
                    {
                        MessageBox.Show("Địa chỉ Email không hợp lệ! Vui lòng nhập đúng định dạng (ví dụ: gv@school.edu.vn).", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    int teacherId = isEdit ? existing!.Id : 0;

                    // ── Duplicate check ──
                    TeacherProfile? dupCode = null;
                    await Task.Run(() =>
                    {
                        dupCode = app.Database.TeacherProfiles
                            .FirstOrDefault(t => t.TeacherCode == code && (!isEdit || t.Id != teacherId));
                    });
                    if (dupCode != null)
                    {
                        MessageBox.Show($"Mã GV \"{code}\" đã tồn tại!\nGV hiện tại: {dupCode.FullName}\n\nVui lòng nhập mã khác.",
                            "⚠️ Trùng mã GV", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Kiểm tra trùng SĐT
                    if (!string.IsNullOrEmpty(phone))
                    {
                        TeacherProfile? dupPhone = null;
                        await Task.Run(() =>
                        {
                            dupPhone = app.Database.TeacherProfiles
                                .FirstOrDefault(t => t.Phone == phone && (!isEdit || t.Id != teacherId));
                        });
                        if (dupPhone != null)
                        {
                            var r = MessageBox.Show($"Số điện thoại \"{phone}\" đã được đăng ký cho giáo viên: {dupPhone.FullName} (Mã: {dupPhone.TeacherCode}).\n\nBạn vẫn muốn tiếp tục sử dụng số điện thoại này?", "⚠️ Trùng Số điện thoại", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (r != MessageBoxResult.Yes) return;
                        }
                    }

                    // Kiểm tra trùng Email
                    if (!string.IsNullOrEmpty(email))
                    {
                        TeacherProfile? dupEmail = null;
                        await Task.Run(() =>
                        {
                            dupEmail = app.Database.TeacherProfiles
                                .FirstOrDefault(t => t.Email == email && (!isEdit || t.Id != teacherId));
                        });
                        if (dupEmail != null)
                        {
                            var r = MessageBox.Show($"Địa chỉ Email \"{email}\" đã được đăng ký cho giáo viên: {dupEmail.FullName} (Mã: {dupEmail.TeacherCode}).\n\nBạn vẫn muốn tiếp tục sử dụng Email này?", "⚠️ Trùng Email", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (r != MessageBoxResult.Yes) return;
                        }
                    }

                    TeacherProfile? dupName = null;
                    await Task.Run(() =>
                    {
                        dupName = app.Database.TeacherProfiles
                            .FirstOrDefault(t => t.FullName == name && (!isEdit || t.Id != teacherId));
                    });
                    if (dupName != null)
                    {
                        var r = MessageBox.Show(
                            $"Đã có GV trùng tên \"{name}\" (Mã: {dupName.TeacherCode}).\n\nBạn vẫn muốn tiếp tục thêm?",
                            "⚠️ Trùng tên GV", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (r != MessageBoxResult.Yes) return;
                    }

                    TeacherProfile tp;
                    if (isEdit)
                    {
                        tp = app.Database.TeacherProfiles.FirstOrDefault(t => t.Id == existing!.Id)!;
                    }
                    else
                    {
                        tp = new TeacherProfile();
                        app.Database.TeacherProfiles.Add(tp);
                    }
                    tp.TeacherCode = code;
                    tp.FullName = name;
                    tp.Title = (cboTitle.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "GV";
                    tp.Subject = txtSubject.Text.Trim();
                    tp.School = txtSchool.Text.Trim();
                    tp.Phone = phone;
                    tp.Email = email;
                    tp.Notes = txtNotes.Text.Trim();
                    tp.IsActive = chkActive.IsChecked == true;
                    tp.UpdatedAt = DateTime.Now;

                    await Task.Run(() => app.Database.SaveChanges());
                    Log.Information("Teacher {Action}: {Code} — {Name}", isEdit ? "updated" : "added", code, name);
                    dlg.Close();
                    LoadTeachers();
                    MessageBox.Show(isEdit ? "✅ Đã cập nhật thông tin!" : "✅ Đã thêm giáo viên thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            btnPanel.Children.Add(btnCancel);
            btnPanel.Children.Add(btnSave);
            sp.Children.Add(btnPanel);

            dlg.Content = new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };

            dlg.ContentRendered += (s, e2) =>
            {
                txtName.Focus();
                if (isEdit) txtName.SelectAll();
            };

            dlg.ShowDialog();
        }

        private static TextBox AddField(StackPanel parent, string label, string value)
        {
            parent.Children.Add(new TextBlock 
            { 
                Text = label, 
                FontSize = 12, 
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(55, 71, 79)),
                Margin = new Thickness(0, 8, 0, 4), 
                FontFamily = new FontFamily("Segoe UI") 
            });
            var tb = new TextBox { Text = value, FontSize = 13, Padding = new Thickness(10, 7, 10, 7), BorderBrush = new SolidColorBrush(Color.FromRgb(0xDE, 0xE2, 0xE6)), FontFamily = new FontFamily("Segoe UI") };
            parent.Children.Add(tb);
            return tb;
        }

        private static ComboBox AddComboField(StackPanel parent, string label, string[] items, string selected)
        {
            parent.Children.Add(new TextBlock 
            { 
                Text = label, 
                FontSize = 12, 
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(55, 71, 79)),
                Margin = new Thickness(0, 8, 0, 4), 
                FontFamily = new FontFamily("Segoe UI") 
            });
            var cb = new ComboBox { FontSize = 13, Padding = new Thickness(8, 5, 8, 5), FontFamily = new FontFamily("Segoe UI") };
            foreach (var item in items)
            {
                var ci = new ComboBoxItem { Content = item };
                if (item == selected) ci.IsSelected = true;
                cb.Items.Add(ci);
            }
            parent.Children.Add(cb);
            return cb;
        }

        // ═══════════════════════════════════════════════════════════
        //  DELETE
        // ═══════════════════════════════════════════════════════════

        private async void DeleteTeacher_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var app = (QASmartTouch.App)Application.Current;
                TeacherProfile? t = null;
                await Task.Run(() =>
                {
                    t = app.Database.TeacherProfiles.FirstOrDefault(x => x.Id == id);
                });
                if (t == null) return;
                var r = MessageBox.Show($"Bạn chắc chắn muốn xóa GV \"{t.FullName}\"?\nMọi dữ liệu liên quan sẽ bị xóa khỏi hệ thống.", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (r == MessageBoxResult.Yes)
                {
                    try
                    {
                        await Task.Run(() =>
                        {
                            app.Database.TeacherProfiles.Remove(t);
                            app.Database.SaveChanges();
                        });
                        Log.Information("Teacher deleted: {Name}", t.FullName);
                        LoadTeachers();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi xóa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  SEARCH & FILTER
        // ═══════════════════════════════════════════════════════════

        private void Search_Changed(object sender, TextChangedEventArgs e)
        {
            if (txtSearch == null || listViewBody == null) return; // Guard against early XAML init
            var txt = txtSearch.Text.Trim();
            if (txt.StartsWith("🔍")) txt = "";
            _searchText = txt;
            LoadTeachers();
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text.StartsWith("🔍")) { txtSearch.Text = ""; txtSearch.Foreground = Brushes.Black; }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text)) { txtSearch.Text = "🔍 Tìm kiếm giáo viên..."; txtSearch.Foreground = new SolidColorBrush(Color.FromRgb(0xAD, 0xB5, 0xBD)); }
        }

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                _filter = tag;
                var active = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));
                var inactive = new SolidColorBrush(Color.FromRgb(0xF1, 0xF3, 0xF5));
                btnFilterAll.Background = tag == "All" ? active : inactive;
                btnFilterAll.Foreground = tag == "All" ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x49, 0x50, 0x57));
                btnFilterActive.Background = tag == "Active" ? active : inactive;
                btnFilterActive.Foreground = tag == "Active" ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x49, 0x50, 0x57));
                btnFilterInactive.Background = tag == "Inactive" ? active : inactive;
                btnFilterInactive.Foreground = tag == "Inactive" ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x49, 0x50, 0x57));
                LoadTeachers();
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadTeachers();

        // ═══════════════════════════════════════════════════════════
        //  IMPORT / EXPORT CSV
        // ═══════════════════════════════════════════════════════════

        private async void ImportCsv_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Nhập danh sách giáo viên từ Excel hoặc CSV",
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
                        dt = ParseTeacherCsvToDataTable(filePath);
                    }
                    else
                    {
                        return (Success: false, Count: 0, Message: "Định dạng file không được hỗ trợ!", Warnings: new List<string>());
                    }

                    if (dt == null || dt.Rows.Count == 0)
                    {
                        return (Success: false, Count: 0, Message: "File dữ liệu không có dòng thông tin hợp lệ nào!", Warnings: new List<string>());
                    }

                    int added = 0;
                    int updated = 0;
                    int maxCodeNum = 0;
                    var phoneRegex = new System.Text.RegularExpressions.Regex(@"^[0-9]{10,11}$");
                    var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                    var warnings = new List<string>();

                    // Quét mã lớn nhất có sẵn trong DB (Tránh lỗi SQL N+1)
                    try
                    {
                        var allDb = app.Database.TeacherProfiles.ToList();
                        foreach (var t in allDb)
                        {
                            if (t.TeacherCode != null && t.TeacherCode.StartsWith("GV"))
                            {
                                if (int.TryParse(t.TeacherCode.Substring(2), out int num) && num > maxCodeNum)
                                    maxCodeNum = num;
                            }
                        }
                    }
                    catch { }

                    using var transaction = app.Database.Database.BeginTransaction();
                    try
                    {
                        int rowNum = 1;
                        foreach (System.Data.DataRow row in dt.Rows)
                        {
                            rowNum++;
                            string name = dt.Columns.Count > 0 ? row[0]?.ToString()?.Trim() ?? "" : "";
                            if (string.IsNullOrWhiteSpace(name)) continue;

                            name = System.Text.RegularExpressions.Regex.Replace(name, @"\s+", " ");

                            string rawTitle = dt.Columns.Count > 1 && !string.IsNullOrWhiteSpace(row[1]?.ToString()) 
                                ? row[1].ToString().Trim() 
                                : "GV";
                            string title = rawTitle.ToLower() switch
                            {
                                "gv" => "GV",
                                "ths" => "ThS",
                                "ts" => "TS",
                                "pgs" => "PGS",
                                "gs" => "GS",
                                _ => rawTitle
                            };
                            string subject = dt.Columns.Count > 2 ? row[2]?.ToString()?.Trim() ?? "" : "";
                            string school = dt.Columns.Count > 3 ? row[3]?.ToString()?.Trim() ?? "" : "";

                            string phone = "";
                            if (dt.Columns.Count > 4 && !string.IsNullOrWhiteSpace(row[4]?.ToString()))
                            {
                                var tempPhone = row[4].ToString().Trim();
                                if (phoneRegex.IsMatch(tempPhone))
                                {
                                    phone = tempPhone;
                                }
                                else
                                {
                                    warnings.Add($"Dòng {rowNum}: SĐT '{tempPhone}' sai định dạng (bỏ qua).");
                                }
                            }

                            string email = "";
                            if (dt.Columns.Count > 5 && !string.IsNullOrWhiteSpace(row[5]?.ToString()))
                            {
                                var tempEmail = row[5].ToString().Trim();
                                if (emailRegex.IsMatch(tempEmail))
                                {
                                    email = tempEmail;
                                }
                                else
                                {
                                    warnings.Add($"Dòng {rowNum}: Email '{tempEmail}' sai định dạng (bỏ qua).");
                                }
                            }

                            string notes = dt.Columns.Count > 6 ? row[6]?.ToString()?.Trim() ?? "" : "";

                            // Khử trùng giáo viên: Tìm theo email (nếu có) hoặc số điện thoại (nếu có)
                            TeacherProfile? existing = null;
                            if (!string.IsNullOrEmpty(email))
                            {
                                existing = app.Database.TeacherProfiles.FirstOrDefault(t => t.Email == email);
                            }
                            if (existing == null && !string.IsNullOrEmpty(phone))
                            {
                                existing = app.Database.TeacherProfiles.FirstOrDefault(t => t.Phone == phone);
                            }

                            if (existing != null)
                            {
                                // Cập nhật hồ sơ giáo viên đã có
                                existing.FullName = name;
                                existing.Title = title;
                                existing.Subject = subject;
                                existing.School = school;
                                if (!string.IsNullOrEmpty(phone)) existing.Phone = phone;
                                if (!string.IsNullOrEmpty(email)) existing.Email = email;
                                existing.Notes = notes;
                                existing.IsActive = true;
                                existing.UpdatedAt = DateTime.Now;
                                updated++;
                            }
                            else
                            {
                                // Thêm giáo viên mới
                                maxCodeNum++;
                                var importCode = $"GV{maxCodeNum:D3}";

                                // Sinh mật khẩu mặc định băm HMACSHA512
                                string defaultPwd = $"Gv@{importCode.Substring(Math.Max(0, importCode.Length - 4))}";
                                string pwdHash = QASmartTouch.Services.AuthenticationService.HashPasswordHMACSHA512(defaultPwd);

                                var tp = new TeacherProfile
                                {
                                    TeacherCode = importCode,
                                    PasswordHash = pwdHash,
                                    FullName = name,
                                    Title = title,
                                    Subject = subject,
                                    School = school,
                                    Phone = phone,
                                    Email = email,
                                    Notes = notes,
                                    IsActive = true,
                                    UpdatedAt = DateTime.Now
                                };
                                app.Database.TeacherProfiles.Add(tp);
                                added++;
                            }
                        }
                        app.Database.SaveChanges();
                        transaction.Commit();
                        return (Success: true, Count: added, Message: $"Đã nhập thành công {added} giáo viên mới, cập nhật {updated} giáo viên!", Warnings: warnings);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return (Success: false, Count: 0, Message: $"Lỗi lưu cơ sở dữ liệu: {ex.Message}", Warnings: new List<string>());
                    }
                });

                if (result.Success)
                {
                    LoadTeachers();
                    string msg = result.Message;
                    if (result.Warnings.Any())
                    {
                        msg += $"\n\n⚠️ Cảnh báo nhập liệu:\n" + string.Join("\n", result.Warnings.Take(5));
                        if (result.Warnings.Count > 5) 
                            msg += $"\n... và {result.Warnings.Count - 5} cảnh báo khác.";
                    }
                    MessageBox.Show(msg, "Import thành công", MessageBoxButton.OK, result.Warnings.Any() ? MessageBoxImage.Warning : MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(result.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi nhập dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static System.Data.DataTable ParseTeacherCsvToDataTable(string filePath)
        {
            var table = new System.Data.DataTable();
            table.Columns.Add("Họ tên");
            table.Columns.Add("Học vị");
            table.Columns.Add("Bộ môn");
            table.Columns.Add("Trường");
            table.Columns.Add("Số điện thoại");
            table.Columns.Add("Email");
            table.Columns.Add("Ghi chú");

            var lines = File.ReadAllLines(filePath, Encoding.UTF8).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            if (lines.Length <= 1) return table;

            foreach (var line in lines.Skip(1))
            {
                var cols = ClassRosterService.ParseCsvLine(line);
                var row = table.NewRow();
                for (int col = 0; col < Math.Min(cols.Length, 7); col++)
                {
                    row[col] = cols[col];
                }
                table.Rows.Add(row);
            }
            return table;
        }

        private async void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Title = "Xuất danh sách giáo viên",
                Filter = "CSV|*.csv",
                FileName = $"DanhSachGV_{DateTime.Now:yyyyMMdd}.csv"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                var filePath = dlg.FileName;
                var app = (QASmartTouch.App)Application.Current;
                
                List<TeacherProfile> teachers = null;
                await Task.Run(() =>
                {
                    teachers = app.Database.TeacherProfiles.OrderBy(t => t.FullName).ToList();
                });

                await Task.Run(() =>
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("TeacherCode,FullName,Title,Subject,School,Phone,Email,Notes");
                    foreach (var t in teachers)
                    {
                        var code = ClassRosterService.SanitizeCsvField(t.TeacherCode);
                        var name = ClassRosterService.SanitizeCsvField(t.FullName);
                        var title = ClassRosterService.SanitizeCsvField(t.Title);
                        var subject = ClassRosterService.SanitizeCsvField(t.Subject);
                        var school = ClassRosterService.SanitizeCsvField(t.School);
                        var phone = ClassRosterService.SanitizeCsvField(t.Phone);
                        var email = ClassRosterService.SanitizeCsvField(t.Email);
                        var notes = ClassRosterService.SanitizeCsvField(t.Notes);

                        sb.AppendLine($"\"{code}\",\"{name}\",\"{title}\",\"{subject}\",\"{school}\",\"{phone}\",\"{email}\",\"{notes}\"");
                    }
                    // WriteAllText with UTF-8 BOM encoding
                    File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(true));
                });

                Log.Information("Exported {Count} teachers to CSV", teachers.Count);
                MessageBox.Show($"✅ Đã xuất {teachers.Count} giáo viên ra tệp CSV thành công!", "Xuất thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất CSV: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static string SanitizeCsvField(string? field) => QASmartClass.Classroom.Services.ClassRosterService.SanitizeCsvField(field);
    }
}
