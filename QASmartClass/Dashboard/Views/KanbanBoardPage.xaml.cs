using QASmartClass.Data;
using QASmartClass.Services;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartClass.Dashboard.Views
{
    public partial class KanbanBoardPage : Page
    {
        private readonly AppDbContext _db;
        private readonly TaskManagementService _taskService;

        public KanbanBoardPage(AppDbContext db)
        {
            InitializeComponent();
            _db = db;
            _taskService = new TaskManagementService(_db);
            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadBoard();
        }

        private void LoadBoard()
        {
            try
            {
                string department = "All";
                if (CbFilter.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
                {
                    department = selectedItem.Tag.ToString() ?? "All";
                }

                var tasks = _taskService.GetTasksByDepartment(department);

                var viewModels = tasks.Select(t => new KanbanTaskViewModel(t)).ToList();

                var todoList = viewModels.Where(t => t.Status == "Todo").ToList();
                var doingList = viewModels.Where(t => t.Status == "InProgress").ToList();
                var doneList = viewModels.Where(t => t.Status == "Done").ToList();

                LvTodo.ItemsSource = todoList;
                LvDoing.ItemsSource = doingList;
                LvDone.ItemsSource = doneList;

                TxtTodoHeader.Text = $"TODO ({todoList.Count})";
                TxtDoingHeader.Text = $"DOING ({doingList.Count})";
                TxtDoneHeader.Text = $"DONE ({doneList.Count})";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi tải bảng Kanban.");
                MessageBox.Show("Lỗi khi tại dữ liệu Kanban.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                LoadBoard();
            }
        }

        private void BtnMoveToDoing_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int taskId)
            {
                if (_taskService.UpdateStatus(taskId, "InProgress"))
                {
                    LoadBoard();
                }
            }
        }

        private void BtnMoveToTodo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int taskId)
            {
                if (_taskService.UpdateStatus(taskId, "Todo"))
                {
                    LoadBoard();
                }
            }
        }

        private void BtnMoveToDone_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int taskId)
            {
                if (_taskService.UpdateStatus(taskId, "Done"))
                {
                    LoadBoard();
                }
            }
        }

        private void BtnNewTask_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("M? form thêm Task m?i ? dây.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public class KanbanTaskViewModel
    {
        private readonly TaskItem _task;
        public KanbanTaskViewModel(TaskItem task) { _task = task; }

        public int Id => _task.Id;
        public string Title => _task.Title;
        public string Priority => _task.Priority;
        public string Status => _task.Status;
        public DateTime Deadline => _task.Deadline;
        public DateTime? CompletedAt => _task.CompletedAt;
        public string AssignedTo => _task.AssignedTo;

        public Brush PriorityBgColor
        {
            get
            {
                return Priority switch
                {
                    "Urgent" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2")), // Light Red
                    "High" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEDD5")), // Light Orange
                    "Medium" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF9C3")), // Light Yellow
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")) // Light Gray
                };
            }
        }

        public Brush PriorityTextColor
        {
            get
            {
                return Priority switch
                {
                    "Urgent" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#991B1B")),
                    "High" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C2410C")),
                    "Medium" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A16207")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"))
                };
            }
        }

        public Brush DeadlineColor
        {
            get
            {
                if (Status != "Done" && Deadline.Date < DateTime.Today)
                {
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")); // Red if overdue
                }
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")); // Gray default
            }
        }
    }
}

