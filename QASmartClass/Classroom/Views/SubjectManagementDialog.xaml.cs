using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.Classroom.Services;
using QASmartClass.Data;

namespace QASmartClass.Classroom.Views
{
    public partial class SubjectManagementDialog : Window
    {
        private readonly SubjectCatalogService _catalog = SubjectCatalogService.Instance;
        private Subject? _selectedSubject;
        private bool _isCreatingNew = false;

        private static readonly string[] PresetIcons = new[]
        {
            "📐", "📖", "🌐", "⚡", "🧪", "🧬", "🔬", "🏛️",
            "🌍", "🗺️", "⚖️", "🤝", "💻", "⚙️", "⚽", "🎵",
            "🎨", "🌟", "🤖", "🎖️", "📚", "✍️", "🎯", "🚀"
        };

        private static readonly string[] PresetColors = new[]
        {
            "#1976D2", "#7B1FA2", "#00838F", "#E64A19", "#2E7D32",
            "#AD1457", "#00897B", "#5D4037", "#00695C", "#4E342E",
            "#F9A825", "#F57F17", "#0277BD", "#558B2F", "#EF6C00",
            "#8E24AA", "#D81B60", "#1565C0", "#33691E", "#37474F"
        };

        public SubjectManagementDialog()
        {
            InitializeComponent();
            InitPresets();
            LoadSubjectList();

            // Select first subject by default
            if (lstSubjects.Items.Count > 0)
            {
                lstSubjects.SelectedIndex = 0;
            }
            else
            {
                PrepareNewSubjectForm();
            }
        }

        private void InitPresets()
        {
            // Preset Icons
            panelIconPresets.Children.Clear();
            foreach (var icon in PresetIcons)
            {
                var btn = new Border
                {
                    Width = 32, Height = 32,
                    CornerRadius = new CornerRadius(6),
                    Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                    Margin = new Thickness(0, 0, 4, 4),
                    Cursor = Cursors.Hand,
                    ToolTip = icon
                };
                btn.Child = new TextBlock
                {
                    Text = icon, FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                string iconChar = icon;
                btn.MouseLeftButtonDown += (s, e) =>
                {
                    txtIcon.Text = iconChar;
                };
                btn.MouseEnter += (s, e) => btn.Background = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                btn.MouseLeave += (s, e) => btn.Background = new SolidColorBrush(Color.FromRgb(241, 245, 249));
                panelIconPresets.Children.Add(btn);
            }

            // Preset Colors
            panelColorPresets.Children.Clear();
            foreach (var hex in PresetColors)
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(hex);
                    var border = new Border
                    {
                        Width = 28, Height = 28,
                        CornerRadius = new CornerRadius(6),
                        Background = new SolidColorBrush(color),
                        Margin = new Thickness(0, 0, 6, 6),
                        Cursor = Cursors.Hand,
                        BorderThickness = new Thickness(1),
                        BorderBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
                        ToolTip = hex
                    };
                    string colorHex = hex;
                    border.MouseLeftButtonDown += (s, e) =>
                    {
                        txtColorHex.Text = colorHex;
                    };
                    panelColorPresets.Children.Add(border);
                }
                catch { }
            }
        }

        private void LoadSubjectList(string? filter = null, int? selectSubjectId = null)
        {
            var subjects = _catalog.GetAllSubjects();
            if (!string.IsNullOrWhiteSpace(filter))
            {
                string f = filter.Trim();
                subjects = subjects.Where(s =>
                    s.Name.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                    s.ShortName.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                    s.DefaultRoom.Contains(f, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            lstSubjects.ItemsSource = subjects;

            if (selectSubjectId.HasValue)
            {
                var target = subjects.FirstOrDefault(s => s.Id == selectSubjectId.Value);
                if (target != null)
                {
                    lstSubjects.SelectedItem = target;
                }
            }
        }

        private void LstSubjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstSubjects.SelectedItem is Subject subject)
            {
                _isCreatingNew = false;
                _selectedSubject = subject;
                PopulateForm(subject);
            }
        }

        private void PopulateForm(Subject subject)
        {
            txtFormTitle.Text = $"Chỉnh sửa: {subject.Name}";
            txtName.Text = subject.Name;
            txtShortName.Text = subject.ShortName;
            txtIcon.Text = subject.Icon;
            txtDefaultRoom.Text = subject.DefaultRoom;
            txtColorHex.Text = subject.ColorHex;
            txtWeeklyPeriods.Text = subject.WeeklyPeriods.ToString();
            txtDisplayOrder.Text = subject.DisplayOrder.ToString();

            btnDelete.Visibility = Visibility.Visible;
            btnDelete.IsEnabled = true;
            btnSave.Content = "💾 Cập nhật môn";
        }

        private void PrepareNewSubjectForm()
        {
            _isCreatingNew = true;
            _selectedSubject = null;
            lstSubjects.SelectedItem = null;

            txtFormTitle.Text = "Thêm Môn học Mới";
            txtName.Text = "";
            txtShortName.Text = "";
            txtIcon.Text = "📚";
            txtDefaultRoom.Text = "P.Học";
            txtColorHex.Text = "#1976D2";
            txtWeeklyPeriods.Text = "2";
            txtDisplayOrder.Text = (_catalog.GetAllSubjects().Count + 1).ToString();

            btnDelete.Visibility = Visibility.Collapsed;
            btnSave.Content = "➕ Tạo môn mới";
            txtName.Focus();
        }

        private void BtnNewSubject_Click(object sender, RoutedEventArgs e)
        {
            PrepareNewSubjectForm();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadSubjectList(txtSearch.Text);
        }

        private void TxtName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isCreatingNew && string.IsNullOrWhiteSpace(txtShortName.Text))
            {
                // Auto generate short name from first letters or first word
                string name = txtName.Text.Trim();
                if (name.Length > 0)
                {
                    var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length == 1)
                    {
                        txtShortName.Text = words[0];
                    }
                    else if (words.Length <= 3)
                    {
                        txtShortName.Text = words[0];
                    }
                }
            }
        }

        private void TxtIcon_TextChanged(object sender, TextChangedEventArgs e)
        {
            string icon = txtIcon.Text.Trim();
            txtSelectedIconPreview.Text = string.IsNullOrEmpty(icon) ? "📚" : icon;
        }

        private void TxtColorHex_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string hex = txtColorHex.Text.Trim();
                if (!hex.StartsWith("#")) hex = "#" + hex;
                if (hex.Length == 7 || hex.Length == 9)
                {
                    var color = (Color)ColorConverter.ConvertFromString(hex);
                    bdColorPreview.Background = new SolidColorBrush(color);
                }
            }
            catch { }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string name = txtName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Vui lòng nhập tên môn học!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus();
                return;
            }

            string shortName = txtShortName.Text.Trim();
            if (string.IsNullOrEmpty(shortName))
            {
                shortName = name;
            }

            string hex = txtColorHex.Text.Trim();
            if (!hex.StartsWith("#")) hex = "#" + hex;
            try
            {
                _ = (Color)ColorConverter.ConvertFromString(hex);
            }
            catch
            {
                hex = "#1976D2";
            }

            int.TryParse(txtWeeklyPeriods.Text.Trim(), out int weekly);
            if (weekly <= 0) weekly = 2;

            int.TryParse(txtDisplayOrder.Text.Trim(), out int order);
            if (order <= 0) order = 1;

            if (_isCreatingNew)
            {
                var newSubj = new Subject
                {
                    Name = name,
                    ShortName = shortName,
                    Icon = string.IsNullOrEmpty(txtIcon.Text.Trim()) ? "📚" : txtIcon.Text.Trim(),
                    ColorHex = hex,
                    DefaultRoom = txtDefaultRoom.Text.Trim(),
                    WeeklyPeriods = weekly,
                    DisplayOrder = order,
                    IsSystem = false
                };

                if (_catalog.AddSubject(newSubj, out string error))
                {
                    txtStatusMsg.Text = $"✅ Đã tạo môn: {name}";
                    LoadSubjectList(txtSearch.Text, newSubj.Id);
                }
                else
                {
                    MessageBox.Show(error, "Lỗi tạo môn học", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else if (_selectedSubject != null)
            {
                _selectedSubject.Name = name;
                _selectedSubject.ShortName = shortName;
                _selectedSubject.Icon = string.IsNullOrEmpty(txtIcon.Text.Trim()) ? "📚" : txtIcon.Text.Trim();
                _selectedSubject.ColorHex = hex;
                _selectedSubject.DefaultRoom = txtDefaultRoom.Text.Trim();
                _selectedSubject.WeeklyPeriods = weekly;
                _selectedSubject.DisplayOrder = order;

                if (_catalog.UpdateSubject(_selectedSubject, out string error))
                {
                    txtStatusMsg.Text = $"✅ Đã lưu cập nhật: {name}";
                    LoadSubjectList(txtSearch.Text, _selectedSubject.Id);
                }
                else
                {
                    MessageBox.Show(error, "Lỗi cập nhật môn học", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSubject == null) return;

            var result = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa môn học '{_selectedSubject.Name}' khỏi danh mục?\nCác tiết học đã xếp trên TKB với tên môn này vẫn được giữ nguyên.",
                "Xác nhận xóa môn học",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                int id = _selectedSubject.Id;
                if (_catalog.DeleteSubject(id, out string error))
                {
                    txtStatusMsg.Text = $"Đã xóa môn: {_selectedSubject.Name}";
                    LoadSubjectList(txtSearch.Text);
                    if (lstSubjects.Items.Count > 0)
                    {
                        lstSubjects.SelectedIndex = 0;
                    }
                    else
                    {
                        PrepareNewSubjectForm();
                    }
                }
                else
                {
                    MessageBox.Show(error, "Lỗi xóa môn học", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Bạn có chắc chắn muốn khôi phục danh mục 20 môn học chuẩn theo chương trình GDPT 2018?\n(Các môn học tự tạo bổ sung sẽ bị xóa)",
                "Khôi phục mặc định",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _catalog.ResetToDefaults();
                LoadSubjectList();
                if (lstSubjects.Items.Count > 0) lstSubjects.SelectedIndex = 0;
                txtStatusMsg.Text = "Đã khôi phục 20 môn học chuẩn GDPT 2018";
            }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
