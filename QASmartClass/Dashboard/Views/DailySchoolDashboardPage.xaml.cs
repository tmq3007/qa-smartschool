using QASmartClass.Data;
using QASmartClass.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace QASmartClass.Dashboard.Views
{
    public partial class DailySchoolDashboardPage : Page
    {
        private readonly AppDbContext _db;
        private readonly DailyTaskService _taskService;
        private readonly string _currentUserId;
        private string _currentDeptFilter = "All";
        private string _myDepartment = "Giáo viên";

        public DailySchoolDashboardPage(AppDbContext db, string currentUserId = "GV01")
        {
            InitializeComponent();
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _taskService = new DailyTaskService(_db);
            _currentUserId = currentUserId;

            // Fetch staff profile to determine department
            var profile = _db.StaffProfiles.FirstOrDefault(s => s.StaffCode == _currentUserId);
            if (profile != null)
            {
                _myDepartment = profile.Department;
            }

            TxtDeptName.Text = $"Bộ phận của tôi: {_myDepartment}";
            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            TxtToday.Text = DateTime.Today.ToString("dd/MM/yyyy");
            LoadAllSchedules();
            CheckSlaStatus();
        }

        private void LoadAllSchedules()
        {
            try
            {
                // 1. School Schedule
                var schoolTasks = _taskService.GetSchoolDailySchedule(DateTime.Today, _currentDeptFilter);
                LvSchoolSchedule.ItemsSource = schoolTasks.Select(t => new DailyTaskItemViewModel(t)).ToList();
                TxtSchoolScheduleHeader.Text = $"Danh sách hoạt động trong ngày ({schoolTasks.Count})";

                // 2. Department Schedule
                var deptTasks = _taskService.GetSchoolDailySchedule(DateTime.Today, _myDepartment);
                LvDeptSchedule.ItemsSource = deptTasks.Select(t => new DailyTaskItemViewModel(t)).ToList();
                TxtDeptHeader.Text = $"Lịch hoạt động của tổ chuyên môn ({deptTasks.Count})";

                // 3. Personal Tasks & Coordinations
                var myTasksAndCoordinations = _taskService.GetMyDailyTasksAndCoordinations(_currentUserId, DateTime.Today);
                
                var myTasks = myTasksAndCoordinations.Where(t => t.AssignedTo == _currentUserId).ToList();
                var myCoordinations = myTasksAndCoordinations.Where(t => t.AssignedTo != _currentUserId).ToList();

                LvMyTasks.ItemsSource = myTasks.Select(t => new DailyTaskItemViewModel(t)).ToList();
                LvMyCoordinations.ItemsSource = myCoordinations.Select(t => new DailyTaskItemViewModel(t)).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi tải dữ liệu DailySchoolDashboardPage");
            }
        }

        private void CheckSlaStatus()
        {
            try
            {
                var windowHoursStr = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Dashboard_ReportWindowHours")?.Value ?? "24";
                double windowHours = double.TryParse(windowHoursStr, out var wh) ? wh : 24;

                var enableSla = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Dashboard_EnableSlaEscalation")?.Value ?? "1";
                if (enableSla == "0")
                {
                    BdrSlaWarning.Visibility = Visibility.Collapsed;
                    return;
                }

                var threshold = DateTime.Now;
                var hasCriticalOverdue = _db.DailyTasks
                    .Any(t => t.IsCritical && t.Status != "Done" && t.Status != "Completed" && t.EndTime != null && t.EndTime.Value.AddHours(windowHours) < threshold);

                if (hasCriticalOverdue)
                {
                    BdrSlaWarning.Visibility = Visibility.Visible;
                    TxtSlaWarningMsg.Text = $"Cảnh báo: Có công việc khẩn cấp (Critical) quá hạn quá {windowHours} giờ chưa hoàn thành! Hệ thống đã tự động gửi báo cáo leo thang cho Hiệu trưởng.";
                }
                else
                {
                    BdrSlaWarning.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Lỗi khi kiểm tra trạng thái SLA");
            }
        }

        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string dept)
            {
                _currentDeptFilter = dept;
                LoadAllSchedules();
            }
        }

        private void BtnReportComplete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int taskId)
            {
                var notes = ShowInputDialog("Báo cáo hoàn thành công việc", "Nhập kết quả thực hiện hoặc sự cố gặp phải:");
                if (notes != null) // null means cancelled
                {
                    bool success = _taskService.CompleteTask(taskId, notes, _currentUserId);
                    if (success)
                    {
                        LoadAllSchedules();
                        CheckSlaStatus();
                        MessageBox.Show("Đã cập nhật báo cáo hoàn thành công việc.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Bạn không có quyền hoàn thành công việc này hoặc công việc không tồn tại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnDelegate_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int taskId)
            {
                var delegation = ShowDelegateDialog();
                if (delegation != null)
                {
                    bool success = _taskService.DelegateTask(taskId, _currentUserId, delegation.Value.delegatee, delegation.Value.comment);
                    if (success)
                    {
                        LoadAllSchedules();
                        MessageBox.Show("Ủy thác công việc thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Lỗi: Ủy thác thất bại. Bạn không có quyền ủy thác công việc này hoặc nhân viên nhận không hợp lệ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private string ShowInputDialog(string title, string prompt)
        {
            string result = null;
            var dialog = new Window
            {
                Title = title,
                Width = 420,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")),
                FontFamily = new FontFamily("Segoe UI")
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var lblPrompt = new TextBlock
            {
                Text = prompt,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(lblPrompt, 0);
            grid.Children.Add(lblPrompt);

            var txtInput = new TextBox
            {
                FontSize = 14,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
                Padding = new Thickness(8),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetRow(txtInput, 1);
            grid.Children.Add(txtInput);

            var stack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 15, 0, 0)
            };
            Grid.SetRow(stack, 2);

            var btnCancel = new Button
            {
                Content = "Hủy bỏ",
                Padding = new Thickness(15, 6, 15, 6),
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569")),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            btnCancel.Click += (s, e) => dialog.Close();
            stack.Children.Add(btnCancel);

            var btnOk = new Button
            {
                Content = "Xác nhận",
                Padding = new Thickness(20, 6, 20, 6),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                IsDefault = true
            };
            btnOk.Click += (s, e) =>
            {
                result = txtInput.Text;
                dialog.Close();
            };
            stack.Children.Add(btnOk);

            grid.Children.Add(stack);
            dialog.Content = grid;
            dialog.ShowDialog();
            return result;
        }

        private (string delegatee, string comment)? ShowDelegateDialog()
        {
            (string delegatee, string comment)? result = null;
            var dialog = new Window
            {
                Title = "Ủy thác công việc",
                Width = 450,
                Height = 280,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")),
                FontFamily = new FontFamily("Segoe UI")
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var lblDelegatee = new TextBlock
            {
                Text = "Mã nhân viên nhận bàn giao:",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569")),
                Margin = new Thickness(0, 0, 0, 4)
            };
            Grid.SetRow(lblDelegatee, 0);
            grid.Children.Add(lblDelegatee);

            var txtDelegatee = new TextBox
            {
                FontSize = 14,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
                Padding = new Thickness(6),
                Margin = new Thickness(0, 0, 0, 12)
            };
            Grid.SetRow(txtDelegatee, 1);
            grid.Children.Add(txtDelegatee);

            var lblComment = new TextBlock
            {
                Text = "Lý do / Hướng dẫn phối hợp:",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569")),
                Margin = new Thickness(0, 0, 0, 4)
            };
            Grid.SetRow(lblComment, 2);
            grid.Children.Add(lblComment);

            var txtComment = new TextBox
            {
                FontSize = 14,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
                Padding = new Thickness(6)
            };
            Grid.SetRow(txtComment, 3);
            grid.Children.Add(txtComment);

            var stack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 15, 0, 0)
            };
            Grid.SetRow(stack, 4);

            var btnCancel = new Button
            {
                Content = "Hủy bỏ",
                Padding = new Thickness(15, 6, 15, 6),
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569")),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            btnCancel.Click += (s, e) => dialog.Close();
            stack.Children.Add(btnCancel);

            var btnOk = new Button
            {
                Content = "Xác nhận",
                Padding = new Thickness(20, 6, 20, 6),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E3A8A")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtDelegatee.Text))
                {
                    MessageBox.Show("Vui lòng nhập mã nhân viên nhận bàn giao.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                result = (txtDelegatee.Text.Trim(), txtComment.Text.Trim());
                dialog.Close();
            };
            stack.Children.Add(btnOk);

            grid.Children.Add(stack);
            dialog.Content = grid;
            dialog.ShowDialog();
            return result;
        }
    }

    public class DailyTaskItemViewModel
    {
        private readonly DailyTask _task;
        public DailyTaskItemViewModel(DailyTask task)
        {
            _task = task;
        }

        public int Id => _task.Id;
        public string Title => _task.Title;
        public string Description => string.IsNullOrEmpty(_task.Description) ? "Không có mô tả chi tiết." : _task.Description;
        public bool IsCritical => _task.IsCritical;

        public string TimeDisplay => string.IsNullOrEmpty(_task.TimeFrame) 
            ? (_task.StartTime?.ToString("HH:mm") ?? "Cả ngày") 
            : _task.TimeFrame;

        public string LocationDisplay => string.IsNullOrEmpty(_task.Location) ? "📍 Không chỉ định địa điểm" : $"📍 {_task.Location}";
        public string AssignedToDisplay => $"👤 Người phụ trách: {_task.AssignedTo}";
        public string CollaboratorsDisplay => string.IsNullOrEmpty(_task.Collaborators) ? "" : $"🤝 Phối hợp: {_task.Collaborators}";
        public string TimeAndLocation => $"{TimeDisplay} | {LocationDisplay}";

        public bool IsNotDone => _task.Status != "Done" && _task.Status != "Completed";

        public string StatusText => _task.Status switch
        {
            "Done" => "✅ Hoàn thành",
            "Completed" => "✅ Hoàn thành",
            "InProgress" => "🔄 Đang thực hiện",
            "Delayed" => "⏳ Trì hoãn",
            _ => "📅 Lên kế hoạch"
        };

        public Brush BgColor => _task.IsCritical 
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF1F2")) // Light red
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")); // Light slate

        public Brush BorderColor => _task.IsCritical 
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FECDD3")) 
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));

        public Brush StatusBg => _task.Status switch
        {
            "Done" or "Completed" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1FAE5")),
            "InProgress" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DBEAFE")),
            "Delayed" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7")),
            _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"))
        };

        public Brush StatusFg => _task.Status switch
        {
            "Done" or "Completed" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#065F46")),
            "InProgress" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E40AF")),
            "Delayed" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#92400E")),
            _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"))
        };
    }
}
