using QASmartClass.Data;
using QASmartClass.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartClass.Leadership.Views
{
    public partial class SchoolCalendarView : Page
    {
        private readonly AppDbContext _db;
        private readonly SchoolCalendarService _calService;
        private DateTime _currentMonday;

        public SchoolCalendarView()
        {
            InitializeComponent();
            _db = new AppDbContext();
Unloaded += (s, e) => { _db?.Dispose(); };
            _calService = new SchoolCalendarService(_db);
            _currentMonday = GetMonday(DateTime.Today);
            Loaded += (_, __) => RenderWeek();
        }

        // Constructor cho dependency injection
        public SchoolCalendarView(AppDbContext db) : this()
        {
            _db = db;
        }

        // --- Navigation ----------------------------------
        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            _currentMonday = _currentMonday.AddDays(-7);
            RenderWeek();
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            _currentMonday = _currentMonday.AddDays(7);
            RenderWeek();
        }

        private void BtnToday_Click(object sender, RoutedEventArgs e)
        {
            _currentMonday = GetMonday(DateTime.Today);
            RenderWeek();
        }

        // --- Render Calendar Grid ------------------------
        private void RenderWeek()
        {
            try
            {
                var sunday = _currentMonday.AddDays(6);
                TxtWeekLabel.Text = $"{_currentMonday:dd/MM}  {sunday:dd/MM/yyyy}";

                var events = _calService.GetEventsByWeek(_currentMonday);

                CalendarGrid.Children.Clear();

                for (int i = 0; i < 7; i++)
                {
                    var date = _currentMonday.AddDays(i);
                    var dayEvents = events.Where(e => e.StartTime.Date == date.Date).ToList();
                    var cell = BuildDayCell(date, dayEvents);
                    CalendarGrid.Children.Add(cell);
                }

                Log.Information("[SchoolCalendar] Rendered week {Mon} with {Count} events",
                    _currentMonday.ToString("dd/MM"), events.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[SchoolCalendar] RenderWeek error");
            }
        }

        private Border BuildDayCell(DateTime date, List<SchoolEvent> events)
        {
            bool isToday = date.Date == DateTime.Today;

            var header = new TextBlock
            {
                Text = date.ToString("dd/MM"),
                FontSize = 13,
                FontWeight = isToday ? FontWeights.Bold : FontWeights.Normal,
                Foreground = isToday
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")),
                Margin = new Thickness(0, 0, 0, 6)
            };

            var panel = new StackPanel();
            panel.Children.Add(header);

            foreach (var ev in events)
            {
                var eventCard = new Border
                {
                    Background = GetEventBrush(ev.Status),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 4, 6, 4),
                    Margin = new Thickness(0, 0, 0, 4),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                var cardPanel = new StackPanel();
                cardPanel.Children.Add(new TextBlock
                {
                    Text = ev.Title,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap
                });
                cardPanel.Children.Add(new TextBlock
                {
                    Text = $"{ev.StartTime:HH:mm} - {ev.EndTime:HH:mm} | {ev.Location}",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Colors.White) { Opacity = 0.85 }
                });

                eventCard.Child = cardPanel;
                eventCard.MouseLeftButtonUp += (_, __) => ShowEventDetail(ev);
                panel.Children.Add(eventCard);
            }

            // Empty state
            if (!events.Any())
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "Không có sự kiện",
                    FontSize = 12,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 0)
                });
            }

            return new Border
            {
                Background = isToday
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFF6FF"))
                    : Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(8),
                Margin = new Thickness(2),
                Child = panel,
                MinHeight = 120
            };
        }

        private Brush GetEventBrush(string status)
        {
            return status switch
            {
                "Meeting"    => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")),
                "Inspection" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
                "Holiday"    => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                "Exam"       => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B5CF6")),
                _            => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
            };
        }

        // --- Show event detail popup ---------------------
        private void ShowEventDetail(SchoolEvent ev)
        {
            var msg = $"Sự kiện: {ev.Title}\n"
                    + $"Thời gian: {ev.StartTime:dd/MM/yyyy HH:mm} - {ev.EndTime:HH:mm}\n"
                    + $"Địa điểm: {ev.Location}\n"
                    + $"Người tổ chức: {ev.Organizer}\n"
                    + $"Trạng thái: {ev.Status}\n\n"
                    + $"Mô tả:\n{ev.Description}";
            MessageBox.Show(msg, "Chi tiết sự kiện", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private List<PeriodConfig> LoadTimetableConfig()
        {
            var list = new List<PeriodConfig>();
            try
            {
                var setting = _db.SystemSettings.Find("Timetable_Periods_Config");
                if (setting != null && !string.IsNullOrEmpty(setting.Value))
                {
                    using (var doc = System.Text.Json.JsonDocument.Parse(setting.Value))
                    {
                        foreach (var elem in doc.RootElement.EnumerateArray())
                        {
                            list.Add(new PeriodConfig
                            {
                                Period = elem.GetProperty("Period").GetInt32(),
                                Start = elem.GetProperty("Start").GetString() ?? string.Empty,
                                End = elem.GetProperty("End").GetString() ?? string.Empty
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi phân tích Timetable_Periods_Config, sử dụng cấu hình mặc định.");
            }

            // Fallback nếu rỗng
            if (!list.Any())
            {
                list.Add(new PeriodConfig { Period = 1, Start = "07:30", End = "08:15" });
                list.Add(new PeriodConfig { Period = 2, Start = "08:20", End = "09:05" });
                list.Add(new PeriodConfig { Period = 3, Start = "09:15", End = "10:00" });
                list.Add(new PeriodConfig { Period = 4, Start = "10:05", End = "10:50" });
                list.Add(new PeriodConfig { Period = 5, Start = "11:00", End = "11:45" });
                list.Add(new PeriodConfig { Period = 6, Start = "13:30", End = "14:15" });
                list.Add(new PeriodConfig { Period = 7, Start = "14:20", End = "15:05" });
                list.Add(new PeriodConfig { Period = 8, Start = "15:15", End = "16:00" });
                list.Add(new PeriodConfig { Period = 9, Start = "16:05", End = "16:50" });
                list.Add(new PeriodConfig { Period = 10, Start = "17:00", End = "17:45" });
            }

            return list;
        }

        public class PeriodConfig
        {
            public int Period { get; set; }
            public string Start { get; set; } = string.Empty;
            public string End { get; set; } = string.Empty;
        }

        // --- Add event dialog ----------------------------
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Window
            {
                Title = "Thêm sự kiện mới",
                Width = 440, Height = 540,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                FontFamily = new FontFamily("Segoe UI"),
                ResizeMode = ResizeMode.NoResize
            };

            var sp = new StackPanel { Margin = new Thickness(20) };

            // Tên sự kiện
            sp.Children.Add(new TextBlock { Text = "Tên sự kiện:", FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
            var txtTitle = new TextBox { FontSize = 14, Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(4) };
            sp.Children.Add(txtTitle);

            // Ngày diễn ra
            sp.Children.Add(new TextBlock { Text = "Ngày diễn ra:", FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
            var dpDate = new DatePicker { FontSize = 14, Margin = new Thickness(0, 0, 0, 10), SelectedDate = DateTime.Today };
            sp.Children.Add(dpDate);

            // Nạp cấu hình Tiết học
            var periods = LoadTimetableConfig();

            // ComboBox chọn Tiết học
            sp.Children.Add(new TextBlock { Text = "Khung giờ theo Tiết học:", FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
            var cboPeriod = new ComboBox { FontSize = 14, Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(4) };
            cboPeriod.Items.Add("Khung giờ tự do (Nhập thủ công)");
            foreach (var p in periods)
            {
                cboPeriod.Items.Add($"Tiết {p.Period} ({p.Start} - {p.End})");
            }
            cboPeriod.SelectedIndex = 0;
            sp.Children.Add(cboPeriod);

            // Giờ bắt đầu
            sp.Children.Add(new TextBlock { Text = "Giờ bắt đầu (HH:mm):", FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
            var txtStart = new TextBox { Text = "08:00", FontSize = 14, Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(4) };
            sp.Children.Add(txtStart);

            // Giờ kết thúc
            sp.Children.Add(new TextBlock { Text = "Giờ kết thúc (HH:mm):", FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
            var txtEnd = new TextBox { Text = "09:00", FontSize = 14, Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(4) };
            sp.Children.Add(txtEnd);

            // Sự kiện thay đổi combobox
            cboPeriod.SelectionChanged += (s, args) =>
            {
                int idx = cboPeriod.SelectedIndex;
                if (idx == 0)
                {
                    txtStart.IsEnabled = true;
                    txtEnd.IsEnabled = true;
                }
                else
                {
                    var selectedP = periods[idx - 1];
                    txtStart.Text = selectedP.Start;
                    txtEnd.Text = selectedP.End;
                    txtStart.IsEnabled = false;
                    txtEnd.IsEnabled = false;
                }
            };

            // Địa điểm
            sp.Children.Add(new TextBlock { Text = "Địa điểm / Phòng họp:", FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
            var txtLocation = new TextBox { FontSize = 14, Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(4) };
            sp.Children.Add(txtLocation);

            // Loại sự kiện
            sp.Children.Add(new TextBlock { Text = "Loại sự kiện:", FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
            var cboStatus = new ComboBox { FontSize = 14, Margin = new Thickness(0, 0, 0, 20), Padding = new Thickness(4) };
            cboStatus.Items.Add("Planned"); cboStatus.Items.Add("Meeting");
            cboStatus.Items.Add("Inspection"); cboStatus.Items.Add("Holiday");
            cboStatus.Items.Add("Exam"); cboStatus.Items.Add("Other");
            cboStatus.SelectedIndex = 0;
            sp.Children.Add(cboStatus);

            // Nút Lưu
            var btnSave = new Button
            {
                Content = "Lưu sự kiện",
                FontSize = 14, Padding = new Thickness(16, 10, 16, 10),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                Foreground = Brushes.White, BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            btnSave.Click += (_, __) =>
            {
                try
                {
                    string title = txtTitle.Text.Trim();
                    if (string.IsNullOrEmpty(title))
                    {
                        MessageBox.Show("Vui lòng nhập tên sự kiện!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var date = dpDate.SelectedDate ?? DateTime.Today;
                    if (!TimeSpan.TryParse(txtStart.Text, out var tsStart))
                    {
                        MessageBox.Show("Định dạng giờ bắt đầu không hợp lệ (HH:mm)!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    if (!TimeSpan.TryParse(txtEnd.Text, out var tsEnd))
                    {
                        MessageBox.Show("Định dạng giờ kết thúc không hợp lệ (HH:mm)!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var startTime = date.Date + tsStart;
                    var endTime = date.Date + tsEnd;

                    if (startTime >= endTime)
                    {
                        MessageBox.Show("Thời gian kết thúc phải sau thời gian bắt đầu!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    string location = txtLocation.Text.Trim();

                    // Kiểm tra trùng lịch tại cùng địa điểm
                    if (!string.IsNullOrEmpty(location))
                    {
                        bool isOverlap = _db.SchoolEvents.Any(e => 
                            e.Location == location && 
                            e.StartTime < endTime && 
                            startTime < e.EndTime
                        );

                        if (isOverlap)
                        {
                            var confirm = MessageBox.Show(
                                $"⚠️ Cảnh báo trùng lặp: Địa điểm '{location}' đã có sự kiện được xếp lịch vào khung giờ này.\nBạn có chắc chắn vẫn muốn lưu sự kiện chứ?",
                                "Xác nhận trùng lặp",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Warning
                            );
                            if (confirm == MessageBoxResult.No)
                            {
                                return;
                            }
                        }
                    }

                    var newEvent = new SchoolEvent
                    {
                        Title = title,
                        StartTime = startTime,
                        EndTime = endTime,
                        Location = location,
                        Status = cboStatus.SelectedItem?.ToString() ?? "Planned",
                        Organizer = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "Ban Giám Hiệu",
                        Department = "All"
                    };

                    var result = _calService.CreateEvent(newEvent);
                    if (result != null)
                    {
                        MessageBox.Show("Đã lưu sự kiện thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        dlg.Close();
                        RenderWeek();
                    }
                    else
                    {
                        MessageBox.Show("Không thể lưu sự kiện. Vui lòng kiểm tra lại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi hệ thống: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            
            sp.Children.Add(btnSave);

            dlg.Content = sp;
            dlg.ShowDialog();
        }

        // --- Helper --------------------------------------
        private static DateTime GetMonday(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff).Date;
        }
    }
}

