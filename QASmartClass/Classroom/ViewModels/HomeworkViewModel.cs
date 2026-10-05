using System;
using QASmartClass.Classroom.Helpers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace QASmartClass.Classroom.ViewModels
{
    public partial class HomeworkViewModel : ObservableObject
    {
        private List<HomeworkItem> _allItems = new();

        [ObservableProperty] private ObservableCollection<HomeworkItemViewModel> filteredItems = new();
        [ObservableProperty] private string currentFilter = "all";
        [ObservableProperty] private string statTotal = "📋 0 bài";
        [ObservableProperty] private string statActive = "🟢 0 đang mở";
        [ObservableProperty] private string statExpired = "⏰ 0 hết hạn";
        [ObservableProperty] private string subtitle = "Giao bài, theo dõi tiến độ nộp bài của học sinh";

        public HomeworkViewModel()
        {
        }

        public void LoadHomework()
        {
            try
            {
                // → ClassroomAppContext
                var list = ClassroomAppContext.Db.Homeworks
                    .OrderByDescending(h => h.CreatedAt)
                    .ToList();

                _allItems.Clear();
                foreach (var h in list)
                {
                    _allItems.Add(new HomeworkItem
                    {
                        Subject = h.Subject,
                        Title = h.Title,
                        Description = h.Description,
                        Deadline = h.Deadline,
                        CreatedAt = h.CreatedAt,
                        Attachment = h.AttachmentPath,
                        EventLogId = h.Id
                    });
                }

                UpdateStats();
                RenderList();
            }
            catch (Exception ex) { Log.Warning("LoadHomework error: {Err}", ex.Message); }
        }

        private void UpdateStats()
        {
            int total = _allItems.Count;
            int active = _allItems.Count(h => h.Deadline >= DateTime.Now);
            int expired = _allItems.Count(h => h.Deadline < DateTime.Now);

            StatTotal = $"📋 {total} bài";
            StatActive = $"🟢 {active} đang mở";
            StatExpired = $"⏰ {expired} hết hạn";
            Subtitle = $"Giao bài, theo dõi tiến độ nộp bài của học sinh  •  {total} bài tập  •  {active} đang mở";
        }

        [RelayCommand]
        private void Filter(string filterMode)
        {
            CurrentFilter = filterMode;
            RenderList();
        }

        private void RenderList()
        {
            var filtered = CurrentFilter switch
            {
                "active" => _allItems.Where(h => h.Deadline >= DateTime.Now).ToList(),
                "expired" => _allItems.Where(h => h.Deadline < DateTime.Now).ToList(),
                _ => _allItems
            };

            FilteredItems.Clear();
            int index = 0;
            foreach (var hw in filtered)
            {
                FilteredItems.Add(new HomeworkItemViewModel(hw, index, this));
                index++;
            }
        }

        [RelayCommand]
        private void CreateHomework()
        {
            ShowHomeworkDialog(null);
        }

        [RelayCommand]
        private void ImportFromWord()
        {
            var ofd = new OpenFileDialog
            {
                Title = "Chọn file Word chứa bài tập",
                Filter = "Word Documents|*.docx;*.doc|Tất cả|*.*"
            };

            if (ofd.ShowDialog() != true) return;

            try
            {
                string filePath = ofd.FileName;
                string fileName = Path.GetFileNameWithoutExtension(filePath);

                string content = ExtractTextFromWord(filePath);

                if (string.IsNullOrWhiteSpace(content))
                {
                    MessageBox.Show("Không thể đọc nội dung file Word.\nFile sẽ được đính kèm dưới dạng file gửi HS.",
                        "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    content = $"(Xem file đính kèm: {Path.GetFileName(filePath)})";
                }

                string subject = "";
                string title = fileName;
                string description = content;
                string deadline = "";
                string notes = "";

                var fields = ParseTemplateFields(content);
                if (fields.Count >= 2)
                {
                    subject = fields.GetValueOrDefault("Môn học", fields.GetValueOrDefault("Mon hoc", "")) ?? "";
                    title = fields.GetValueOrDefault("Tiêu đề", fields.GetValueOrDefault("Tieu de", "")) ?? "";
                    description = fields.GetValueOrDefault("Nội dung", fields.GetValueOrDefault("Noi dung", "")) ?? "";
                    deadline = fields.GetValueOrDefault("Hạn nộp", fields.GetValueOrDefault("Han nop", "")) ?? "";
                    notes = fields.GetValueOrDefault("Ghi chú", fields.GetValueOrDefault("Ghi chu", "")) ?? "";

                    if (!string.IsNullOrWhiteSpace(notes) && !string.IsNullOrWhiteSpace(description))
                        description = description + "\n\n📌 Ghi chú: " + notes;
                }
                else
                {
                    var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length > 1)
                    {
                        title = lines[0].Trim();
                        description = string.Join("\n", lines.Skip(1)).Trim();
                    }
                }

                DateTime? parsedDeadline = null;
                bool parseDeadlineFailed = false;
                if (!string.IsNullOrWhiteSpace(deadline))
                {
                    string[] formats = { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "dd/MM/yyyy HH:mm" };
                    if (DateTime.TryParseExact(deadline.Trim(), formats,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var dt))
                    {
                        parsedDeadline = dt;
                    }
                    else
                    {
                        parseDeadlineFailed = true;
                    }
                }

                if (parseDeadlineFailed)
                {
                    MessageBox.Show($"Không thể nhận diện định dạng ngày hạn nộp '{deadline}' trong file Word.\n\nHệ thống đã đặt ngày hạn nộp mặc định là 3 ngày sau. Vui lòng kiểm tra và sửa lại.", 
                        "Cảnh báo định dạng hạn nộp", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                var dummyItem = new HomeworkItem
                {
                    Title = title,
                    Description = description,
                    Subject = string.IsNullOrWhiteSpace(subject) ? "Toán" : subject,
                    Deadline = parsedDeadline ?? DateTime.Today.AddDays(3),
                    Attachment = filePath
                };

                ShowHomeworkDialog(dummyItem, isImport: true, importFieldsCount: fields.Count, parseDeadlineFailed: parseDeadlineFailed || parsedDeadline == null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi import: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Warning("ImportFromWord error: {Err}", ex.Message);
            }
        }

        private void ShowHomeworkDialog(HomeworkItem? existing, bool isImport = false, int importFieldsCount = 0, bool parseDeadlineFailed = false)
        {
            bool isEdit = existing != null && !isImport;

            var dlg = new Window
            {
                Title = isEdit ? "✏️ Chỉnh sửa bài tập" : (isImport ? "📄 Nhập bài tập từ Word" : "📝 Giao bài tập về nhà"),
                Width = 620, Height = 650,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(250, 251, 252))
            };

            var mainScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(0)
            };

            var sp = new StackPanel { Margin = new Thickness(24, 20, 24, 20) };

            if (isImport)
            {
                var infoBanner = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(232, 245, 233)),
                    CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10, 14, 10),
                    Margin = new Thickness(0, 0, 0, 16)
                };
                var infoSp = new StackPanel { Orientation = Orientation.Horizontal };
                infoSp.Children.Add(new TextBlock { Text = "📄", FontSize = 18, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center });
                var infoTextSp = new StackPanel();
                infoTextSp.Children.Add(new TextBlock
                {
                    Text = $"Đã đọc file: {Path.GetFileName(existing?.Attachment)}",
                    FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50))
                });
                infoTextSp.Children.Add(new TextBlock
                {
                    Text = importFieldsCount >= 2
                        ? "✅ Phát hiện file mẫu QA Smart Class! Các trường đã được tự động điền."
                        : "Nội dung đã được trích xuất. Kiểm tra và chỉnh sửa nếu cần.",
                    FontSize = 11.5, Foreground = new SolidColorBrush(Color.FromRgb(102, 187, 106))
                });
                infoSp.Children.Add(infoTextSp);
                infoBanner.Child = infoSp;
                sp.Children.Add(infoBanner);
            }
            else
            {
                var headerBorder = new Border
                {
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(18, 14, 18, 14),
                    Margin = new Thickness(0, 0, 0, 20)
                };
                headerBorder.Background = new LinearGradientBrush(
                    Color.FromRgb(21, 101, 192), Color.FromRgb(30, 136, 229), 0);
                var headerSp = new StackPanel();
                headerSp.Children.Add(new TextBlock
                {
                    Text = isEdit ? "✏️ Chỉnh Sửa Bài Tập" : "📝 Giao Bài Tập Về Nhà",
                    FontSize = 19, FontWeight = FontWeights.Bold, Foreground = Brushes.White
                });
                headerSp.Children.Add(new TextBlock
                {
                    Text = isEdit ? "Cập nhật thông tin bài tập" : "Tạo và gửi bài tập đến học sinh",
                    FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(179, 217, 255)),
                    Margin = new Thickness(0, 3, 0, 0)
                });
                headerBorder.Child = headerSp;
                sp.Children.Add(headerBorder);
            }

            sp.Children.Add(CreateFieldLabel("📚 Môn học", true));
            var cboSubject = new ComboBox
            {
                FontSize = 14, Padding = new Thickness(10, 8, 10, 8),
                IsEditable = true, Margin = new Thickness(0, 0, 0, 12)
            };
            string[] defaultSubjects = { "Toán", "Ngữ Văn", "Tiếng Anh", "Vật Lý", "Hóa Học", "Sinh Học", "Lịch Sử", "Địa Lý", "GDCD", "Tin Học", "Công Nghệ", "Thể Dục", "Âm Nhạc", "Mỹ Thuật", "Khoa học tự nhiên", "Lịch sử và Địa lý", "Hoạt động trải nghiệm" };
            foreach (var s in defaultSubjects) cboSubject.Items.Add(s);
            cboSubject.Text = existing?.Subject ?? "Toán";
            sp.Children.Add(cboSubject);

            sp.Children.Add(CreateFieldLabel("📋 Tiêu đề bài tập", true));
            var txtTitle = new TextBox
            {
                Text = existing?.Title ?? "",
                FontSize = 14, Padding = new Thickness(12, 9, 12, 9),
                Margin = new Thickness(0, 0, 0, 12),
                MaxLength = 100
            };
            sp.Children.Add(txtTitle);

            sp.Children.Add(CreateFieldLabel("📝 Nội dung / Yêu cầu"));
            var txtDesc = new TextBox
            {
                Text = existing?.Description ?? "",
                FontSize = 13.5, Padding = new Thickness(12, 9, 12, 9),
                AcceptsReturn = true, MinHeight = 110, MaxHeight = 160,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 12),
                MaxLength = 2000
            };
            sp.Children.Add(txtDesc);

            var deadlineGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            deadlineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            deadlineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            deadlineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var deadlineSp = new StackPanel();
            deadlineSp.Children.Add(CreateFieldLabel("📅 Ngày hạn nộp", true));
            var dpDeadline = new DatePicker
            {
                SelectedDate = (isImport && parseDeadlineFailed) ? (DateTime?)null : (existing?.Deadline.Date ?? DateTime.Today.AddDays(3)),
                FontSize = 13.5,
                Height = 36
            };
            deadlineSp.Children.Add(dpDeadline);
            Grid.SetColumn(deadlineSp, 0);
            deadlineGrid.Children.Add(deadlineSp);

            var timeSp = new StackPanel();
            timeSp.Children.Add(CreateFieldLabel("⏰ Giờ hạn nộp"));
            var cboTime = new ComboBox { FontSize = 13.5, Padding = new Thickness(10, 8, 10, 8) };
            string[] times = { "07:00", "08:00", "09:00", "10:00", "11:00", "12:00", "14:00", "15:00", "16:00", "17:00", "18:00", "19:00", "20:00", "21:00", "22:00", "23:59" };
            foreach (var t in times) cboTime.Items.Add(t);
            cboTime.SelectedIndex = existing != null
                ? Math.Max(0, Array.IndexOf(times, existing.Deadline.ToString("HH:mm")))
                : 12;
            timeSp.Children.Add(cboTime);
            Grid.SetColumn(timeSp, 2);
            deadlineGrid.Children.Add(timeSp);

            sp.Children.Add(deadlineGrid);

            var chkNoDeadline = new CheckBox
            {
                Content = "🔓 Không giới hạn hạn nộp bài",
                FontSize = 13.5,
                Margin = new Thickness(0, 0, 0, 12),
                VerticalContentAlignment = VerticalAlignment.Center
            };

            chkNoDeadline.Checked += (s, e) =>
            {
                dpDeadline.IsEnabled = false;
                cboTime.IsEnabled = false;
                dpDeadline.SelectedDate = null;
            };
            chkNoDeadline.Unchecked += (s, e) =>
            {
                dpDeadline.IsEnabled = true;
                cboTime.IsEnabled = true;
                dpDeadline.SelectedDate = existing?.Deadline.Date ?? DateTime.Today.AddDays(3);
            };

            bool hasNoDeadlineInitial = existing != null && existing.Deadline.Year >= 2090;
            chkNoDeadline.IsChecked = hasNoDeadlineInitial;
            if (hasNoDeadlineInitial)
            {
                dpDeadline.IsEnabled = false;
                cboTime.IsEnabled = false;
                dpDeadline.SelectedDate = null;
            }

            sp.Children.Add(chkNoDeadline);

            sp.Children.Add(CreateFieldLabel("📎 File đính kèm (tùy chọn)"));
            var attachGrid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
            attachGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            attachGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var txtAttachment = new TextBox
            {
                Text = existing?.Attachment ?? "",
                FontSize = 13.5, Padding = new Thickness(12, 9, 12, 9),
                IsReadOnly = true,
                Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetColumn(txtAttachment, 0);
            attachGrid.Children.Add(txtAttachment);

            var btnBrowse = new Button
            {
                Content = "📂 Chọn file",
                FontSize = 13, Padding = new Thickness(14, 8, 14, 8),
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(222, 226, 230)),
                Cursor = Cursors.Hand
            };
            btnBrowse.Click += (_, _) =>
            {
                var ofd = new OpenFileDialog
                {
                    Title = "Chọn file đính kèm",
                    Filter = "Tất cả|*.*|Word|*.docx;*.doc|PDF|*.pdf|Ảnh|*.png;*.jpg;*.jpeg"
                };
                if (ofd.ShowDialog() == true)
                    txtAttachment.Text = ofd.FileName;
            };
            Grid.SetColumn(btnBrowse, 1);
            attachGrid.Children.Add(btnBrowse);
            sp.Children.Add(attachGrid);

            var btnPanel = new DockPanel { Margin = new Thickness(0, 4, 0, 0) };

            var btnCancel = new Button
            {
                Content = "❌ Hủy",
                FontSize = 14, Padding = new Thickness(24, 11, 24, 11),
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                IsCancel = true
            };
            btnCancel.Click += (_, _) => dlg.DialogResult = false;
            DockPanel.SetDock(btnCancel, Dock.Right);
            btnPanel.Children.Add(btnCancel);

            var btnSave = new Button
            {
                Content = isEdit ? "💾 Cập nhật" : "📤 Giao bài & Gửi HS",
                FontSize = 14, FontWeight = FontWeights.Bold,
                Padding = new Thickness(24, 11, 24, 11),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand, IsDefault = true
            };
            btnSave.Background = new LinearGradientBrush(
                Color.FromRgb(67, 160, 71), Color.FromRgb(102, 187, 106), 0);

            // Validations inside save button click event
            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtTitle.Text))
                {
                    MessageBox.Show("Vui lòng nhập tiêu đề bài tập!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (chkNoDeadline.IsChecked == true)
                {
                    dlg.DialogResult = true;
                    return;
                }

                if (dpDeadline.SelectedDate == null)
                {
                    MessageBox.Show("Hạn nộp bài tập từ file Word không hợp lệ hoặc chưa được chọn. Vui lòng chọn Ngày hạn nộp bài hợp lệ!", 
                        "Thiếu thông tin hạn nộp", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var deadlineDate = dpDeadline.SelectedDate.Value;
                var timeStr = (cboTime.SelectedItem?.ToString() ?? cboTime.Text).Trim();
                
                // Regex validation for HH:mm time format
                if (!System.Text.RegularExpressions.Regex.IsMatch(timeStr, @"^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]$"))
                {
                    MessageBox.Show("Định dạng giờ nộp bài không hợp lệ! Vui lòng nhập đúng dạng HH:mm (ví dụ: 17:00, 23:59).", 
                        "Lỗi định dạng giờ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var timeParts = timeStr.Split(':');
                var deadline = new DateTime(deadlineDate.Year, deadlineDate.Month, deadlineDate.Day,
                    int.Parse(timeParts[0]), int.Parse(timeParts[1]), 0);

                // Block deadline in the past
                if (deadline <= DateTime.Now)
                {
                    MessageBox.Show("Hạn nộp bài tập phải nằm trong tương lai! Vui lòng chọn lại ngày hoặc giờ.", 
                        "Hạn nộp không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                dlg.DialogResult = true;
            };

            DockPanel.SetDock(btnSave, Dock.Right);
            btnPanel.Children.Add(btnSave);

            sp.Children.Add(btnPanel);

            mainScroll.Content = sp;
            dlg.Content = mainScroll;

            if (dlg.ShowDialog() != true) return;

            // Form is successfully validated and closed here
            DateTime finalDeadline;
            if (chkNoDeadline.IsChecked == true)
            {
                finalDeadline = new DateTime(2099, 12, 31, 23, 59, 0);
            }
            else
            {
                var deadlineDateVal = dpDeadline.SelectedDate!.Value;
                var timeStrVal = (cboTime.SelectedItem?.ToString() ?? cboTime.Text).Trim();
                var timePartsVal = timeStrVal.Split(':');
                finalDeadline = new DateTime(deadlineDateVal.Year, deadlineDateVal.Month, deadlineDateVal.Day,
                    int.Parse(timePartsVal[0]), int.Parse(timePartsVal[1]), 0);
            }

            if (isEdit)
            {
                existing!.Subject = cboSubject.Text.Trim();
                existing.Title = txtTitle.Text.Trim();
                existing.Description = txtDesc.Text.Trim();
                existing.Deadline = finalDeadline;
                existing.Attachment = txtAttachment.Text.Trim();

                SaveHomeworkToDb(existing, true);
                RenderList();
                UpdateStats();
                MessageBox.Show($"✅ Đã cập nhật bài tập!\n\n📝 {existing.Title}", "Cập nhật thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var newHw = new HomeworkItem
                {
                    Subject = cboSubject.Text.Trim(),
                    Title = txtTitle.Text.Trim(),
                    Description = txtDesc.Text.Trim(),
                    Deadline = finalDeadline,
                    Attachment = txtAttachment.Text.Trim(),
                    CreatedAt = DateTime.Now
                };

                SaveHomeworkToDb(newHw, false);
                _allItems.Insert(0, newHw);
                RenderList();
                UpdateStats();

                // → ClassroomAppContext
                var net = ClassroomAppContext.Network;
                
                string cleanSubject = newHw.Subject.Replace("|", " ");
                string cleanTitle = newHw.Title.Replace("|", " ").Replace("\n", " ");
                string cleanDesc = newHw.Description.Replace("|", " ");
                string deadlineStr = newHw.Deadline.ToString("yyyy-MM-dd HH:mm");
                string attachmentFile = !string.IsNullOrEmpty(newHw.Attachment) ? Path.GetFileName(newHw.Attachment).Replace("|", " ") : "";

                var cmd = $"CMD|ASSIGNMENT|{newHw.EventLogId}|{cleanSubject}|{cleanTitle}|{deadlineStr}|{cleanDesc}|{attachmentFile}";
                
                if (net?.IsBroadcasting == true)
                    _ = net.SendCommandAsync(cmd);
                else
                    ClassroomAppContext.DispatchCommand(cmd);

                MessageBox.Show($"✅ Đã giao BTVN và gửi đến HS!\n\n📝 {newHw.Title}\n📚 {newHw.Subject}\n⏰ Hạn: {newHw.Deadline:dd/MM/yyyy HH:mm}",
                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private TextBlock CreateFieldLabel(string text, bool required = false)
        {
            var tb = new TextBlock
            {
                FontSize = 13.5, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(73, 80, 87)),
                Margin = new Thickness(0, 0, 0, 5)
            };
            tb.Inlines.Add(text);
            if (required)
            {
                tb.Inlines.Add(new System.Windows.Documents.Run(" *") { Foreground = Brushes.Red, FontSize = 13 });
            }
            return tb;
        }

        [RelayCommand]
        public void EditHomework(HomeworkItem hw)
        {
            if (hw != null) ShowHomeworkDialog(hw);
        }

        [RelayCommand]
        public void DeleteHomework(HomeworkItem hw)
        {
            if (hw == null) return;
            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa bài tập này?\n\n📝 {hw.Title}\n⚠️ Thao tác này sẽ xóa cả dữ liệu nộp bài của HS.",
                "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    if (hw.EventLogId > 0)
                    {
                        // → ClassroomAppContext
                        var existing = ClassroomAppContext.Db.Homeworks.Find(hw.EventLogId);
                        if (existing != null)
                        {
                            ClassroomAppContext.Db.Homeworks.Remove(existing);
                            ClassroomAppContext.Db.SaveChanges();
                        }
                    }

                    _allItems.Remove(hw);
                    RenderList();
                    UpdateStats();

                    Log.Information("Homework deleted: {Title}", hw.Title);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xóa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveHomeworkToDb(HomeworkItem hw, bool isUpdate)
        {
            try
            {
                // → ClassroomAppContext

                if (isUpdate && hw.EventLogId > 0)
                {
                    var existing = ClassroomAppContext.Db.Homeworks.Find(hw.EventLogId);
                    if (existing != null)
                    {
                        existing.Subject = hw.Subject;
                        existing.Title = hw.Title;
                        existing.Description = hw.Description;
                        existing.Deadline = hw.Deadline;
                        existing.AttachmentPath = hw.Attachment ?? string.Empty;
                        ClassroomAppContext.Db.SaveChanges();
                    }
                }
                else
                {
                    var newHw = new QASmartClass.Data.Homework
                    {
                        Subject = hw.Subject,
                        Title = hw.Title,
                        Description = hw.Description,
                        Deadline = hw.Deadline,
                        CreatedAt = DateTime.Now,
                        AttachmentPath = hw.Attachment ?? string.Empty,
                        ClassId = ""
                    };
                    ClassroomAppContext.Db.Homeworks.Add(newHw);
                    ClassroomAppContext.Db.SaveChanges();
                    hw.EventLogId = newHw.Id; // Sync newly created database ID
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string ExtractTextFromWord(string filePath)
        {
            if (!System.IO.File.Exists(filePath)) return "";
            try
            {
                using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(filePath, false);
                var body = doc.MainDocumentPart?.Document?.Body;
                if (body == null) return "";

                var sb = new System.Text.StringBuilder();
                foreach (var para in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                {
                    string text = para.InnerText;
                    if (!string.IsNullOrEmpty(text))
                    {
                        sb.AppendLine(text);
                    }
                }
                return sb.ToString().Trim();
            }
            catch (Exception ex)
            {
                Log.Warning("ExtractTextFromWord error: {Err}", ex.Message);
                return "";
            }
        }

        private Dictionary<string, string> ParseTemplateFields(string content)
        {
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(content)) return fields;

            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string currentKey = "";
            string currentValue = "";

            foreach (var line in lines)
            {
                if (line.Contains(":") && line.Length < 100)
                {
                    int idx = line.IndexOf(":");
                    string k = line.Substring(0, idx).Trim();
                    string v = line.Substring(idx + 1).Trim();

                    if (!string.IsNullOrEmpty(currentKey))
                        fields[currentKey] = currentValue.Trim();

                    currentKey = k;
                    currentValue = v;
                }
                else
                {
                    if (!string.IsNullOrEmpty(currentKey))
                        currentValue += "\n" + line.Trim();
                }
            }

            if (!string.IsNullOrEmpty(currentKey))
                fields[currentKey] = currentValue.Trim();

            return fields;
        }

        [RelayCommand]
        public void DownloadTemplate()
        {
            try
            {
                var sfd = new SaveFileDialog
                {
                    Title = "Lưu file mẫu bài tập",
                    Filter = "Word Document|*.docx",
                    FileName = "QASmartClass_MauBaiTap.docx"
                };

                if (sfd.ShowDialog() == true)
                {
                    var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Templates", "HomeworkTemplate.docx");
                    if (File.Exists(templatePath))
                    {
                        File.Copy(templatePath, sfd.FileName, true);
                        MessageBox.Show($"Đã tải file mẫu thành công!\n\nLưu tại: {sfd.FileName}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{sfd.FileName}\"");
                    }
                    else
                    {
                        MessageBox.Show("Không tìm thấy file mẫu trong thư mục cài đặt.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class HomeworkItem
    {
        public string Subject { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Deadline { get; set; } = DateTime.Today.AddDays(3);
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Attachment { get; set; } = string.Empty;
        public int EventLogId { get; set; } = 0;
    }

    public class HomeworkItemViewModel : ObservableObject
    {
        public HomeworkItem Item { get; }
        public int Index { get; }
        public string DisplayIndex => $"#{Index + 1}";
        private readonly HomeworkViewModel _parent;

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public bool IsOverdue => Item.Deadline < DateTime.Now;
        public string AccentColor => IsOverdue ? "#6C757D" : "#2E7D32";
        public string StatusText => IsOverdue ? "⏰ Đã hết hạn" : "🟢 Đang mở";
        public string BgBorder => IsOverdue ? "#F8F9FA" : "#E8F5E9";
        public string BorderBrush => IsOverdue ? "#DEE2E6" : "#C8E6C9";
        public string TimeInfo
        {
            get
            {
                if (IsOverdue)
                {
                    var elapsed = DateTime.Now - Item.Deadline;
                    if (elapsed.TotalDays >= 1) return $"Hết hạn {elapsed.Days} ngày trước";
                    if (elapsed.TotalHours >= 1) return $"Hết hạn {elapsed.Hours}h trước";
                    return $"Hết hạn {elapsed.Minutes} phút trước";
                }
                else
                {
                    var remaining = Item.Deadline - DateTime.Now;
                    if (remaining.TotalDays >= 1) return $"Còn {remaining.Days} ngày";
                    if (remaining.TotalHours >= 1) return $"Còn {remaining.Hours}h {remaining.Minutes}p";
                    return $"Còn {remaining.Minutes}p {remaining.Seconds}s";
                }
            }
        }
        
        public string TimeInfoBg => "#FFF8E1";
        public string TimeInfoFg => "#F57F17";

        public string DeadlineBg => IsOverdue ? "#E9ECEF" : "#E8F5E9";
        public string DeadlineFg => IsOverdue ? "#495057" : "#2E7D32";

        public ICommand EditCommand => _parent.EditHomeworkCommand;
        public ICommand DeleteCommand => _parent.DeleteHomeworkCommand;
        public ICommand ResendCommand { get; }
        public ICommand ToggleExpandCommand { get; }

        public HomeworkItemViewModel(HomeworkItem item, int index, HomeworkViewModel parent)
        {
            Item = item;
            Index = index;
            _parent = parent;
            
            ToggleExpandCommand = new RelayCommand(() => IsExpanded = !IsExpanded);

            ResendCommand = new RelayCommand(() =>
            {
                // → ClassroomAppContext
                var net = ClassroomAppContext.Network;
                
                string cleanSubject = Item.Subject.Replace("|", " ");
                string cleanTitle = Item.Title.Replace("|", " ").Replace("\n", " ");
                string cleanDesc = Item.Description.Replace("|", " ");
                string deadlineStr = Item.Deadline.ToString("yyyy-MM-dd HH:mm");
                string attachmentFile = !string.IsNullOrEmpty(Item.Attachment) ? Path.GetFileName(Item.Attachment).Replace("|", " ") : "";

                var cmd = $"CMD|ASSIGNMENT|{Item.EventLogId}|{cleanSubject}|{cleanTitle}|{deadlineStr}|{cleanDesc}|{attachmentFile}";
                
                if (net?.IsBroadcasting == true)
                    _ = net.SendCommandAsync(cmd);
                else
                    ClassroomAppContext.DispatchCommand(cmd);
                MessageBox.Show($"✅ Đã gửi lại BTVN cho HS!\n\n📝 {Item.Title}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }
    }
}
