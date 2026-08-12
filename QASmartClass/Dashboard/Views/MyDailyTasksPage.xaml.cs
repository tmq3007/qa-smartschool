﻿using QASmartClass.Data;
using QASmartClass.Services;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartClass.Dashboard.Views
{
    public partial class MyDailyTasksPage : Page
    {
        private readonly AppDbContext _db;
        private readonly DailyTaskService _taskService;
        private readonly string _currentUserId;

        public MyDailyTasksPage(AppDbContext db)
        {
            InitializeComponent();
            _db = db;
            _taskService = new DailyTaskService(_db);
            
            // Theo thi?t k?, currentUserId l?y t? App.UserRoleService.
            // Đ? demo, gi? l?p l?y ID ngu?i dùng hi?n tại
            _currentUserId = "GV_NguyenVanA"; 

            // Add BooleanToVisibilityConverter if not exists
            if (!Resources.Contains("BooleanToVisibilityConverter"))
            {
                Resources.Add("BooleanToVisibilityConverter", new BooleanToVisibilityConverter());
            }

            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            TxtCurrentDate.Text = DateTime.Today.ToString("dd/MM/yyyy");
            LoadTasks();
        }

        private void LoadTasks()
        {
            try
            {
                var tasks = _taskService.GetMyTasks(_currentUserId, DateTime.Today);

                var viewModels = tasks.Select(t => new DailyTaskViewModel(t)).ToList();

                var todoList = viewModels.Where(t => t.Status == "Pending").ToList();
                var inProgressList = viewModels.Where(t => t.Status == "InProgress").ToList();
                var doneList = viewModels.Where(t => t.Status == "Done").ToList();

                LvTodo.ItemsSource = todoList;
                LvInProgress.ItemsSource = inProgressList;
                LvDone.ItemsSource = doneList;

                TxtTodoHeader.Text = $"📝 Cần làm ({todoList.Count})";
                TxtInProgressHeader.Text = $"⏳ Đang làm ({inProgressList.Count})";
                TxtDoneHeader.Text = $"✅ Đã xong ({doneList.Count})";

                int total = tasks.Count;
                int done = doneList.Count;

                if (total == 0)
                {
                    BdrEmptyState.Visibility = Visibility.Visible;
                    TxtProgressLabel.Text = "Hoàn thành: 0/0 (0%)";
                    PbTaskProgress.Value = 0;
                }
                else
                {
                    BdrEmptyState.Visibility = Visibility.Collapsed;
                    double pct = (double)done * 100 / total;
                    TxtProgressLabel.Text = $"Hoàn thành: {done}/{total} ({pct:F0}%)";
                    PbTaskProgress.Value = pct;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi tải danh sách công việc");
                MessageBox.Show("Có lỗi xảy ra khi tại dữ liệu.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnStartTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int taskId)
            {
                try
                {
                    var task = _db.DailyTasks.Find(taskId);
                    if (task != null)
                    {
                        task.Status = "InProgress";
                        _db.SaveChanges();
                        LoadTasks();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Lỗi khi chuyển trạng thái Task");
                }
            }
        }

        private void BtnCompleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int taskId)
            {
                // TODO: T?o m?t custom dialog nh?p notes trong th?c t?
                // ? dây ta dùng Service d? complete luôn v?i ghi chú r?ng cho demo nhanh
                bool success = _taskService.CompleteTask(taskId, "Hoàn thành t?t");
                if (success)
                {
                    LoadTasks();
                }
                else
                {
                    MessageBox.Show("Không thể hoàn thành công việc.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class DailyTaskViewModel
    {
        private readonly DailyTask _task;
        public DailyTaskViewModel(DailyTask task) { _task = task; }

        public int Id => _task.Id;
        public string Title => _task.Title;
        public string Status => _task.Status;
        public DateTime DueDate => _task.DueDate;
        public string AssignedBy => _task.AssignedBy;
        public string Notes => _task.Notes;

        public bool IsOverdue => _task.DueDate.Date < DateTime.Today && Status != "Done";
        public bool HasNotes => !string.IsNullOrWhiteSpace(_task.Notes);

        public Brush OverdueColor
        {
            get
            {
                if (IsOverdue)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")); // Red
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E3A5F")); // Default Dark Blue
            }
        }
    }
}

