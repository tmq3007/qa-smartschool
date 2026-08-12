using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QASmartClass.Data;

namespace QASmartClass.TeacherHub.Views
{
    public partial class MyTasksView : Page
    {
        private AppDbContext _db;
        private DailyTask? _draggedTask;

        public MyTasksView()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;

            // S2-05: Enable drag-and-drop cho 3 cột Kanban
            lstPending.AllowDrop = true;
            lstInProgress.AllowDrop = true;
            lstDone.AllowDrop = true;

            lstPending.Drop += (s, e) => HandleDrop("Pending");
            lstInProgress.Drop += (s, e) => HandleDrop("InProgress");
            lstDone.Drop += (s, e) => HandleDrop("Done");

            lstPending.DragOver += AllowDropHandler;
            lstInProgress.DragOver += AllowDropHandler;
            lstDone.DragOver += AllowDropHandler;

            lstPending.PreviewMouseLeftButtonDown += StartDrag;
            lstInProgress.PreviewMouseLeftButtonDown += StartDrag;
            lstDone.PreviewMouseLeftButtonDown += StartDrag;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try { _db = new AppDbContext(); } catch { }
            LoadBoard();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _db?.Dispose();
            _db = null;
        }

        private void LoadBoard()
        {
            if (_db == null) return;
            
            string currentUser = QASmartClass.Staff.Services.StaffSession.DisplayName ?? "GUEST";
            var allTasks = _db.DailyTasks.ToList();
            
            // Tìm task được giao chính xác cho user này, hoặc lấy task chung (All/Department)
            var userTasks = allTasks.Where(t => t.AssignedTo == currentUser || t.AssignedTo == "All" || t.AssignedTo == QASmartClass.Staff.Services.StaffSession.Role).ToList();
            var displayTasks = userTasks.Any() ? userTasks : allTasks.Take(20).ToList(); // Fallback nếu user chưa có task nào

            lstPending.ItemsSource = displayTasks.Where(t => t.Status == "Pending").OrderBy(t => t.DueDate).ToList();
            lstInProgress.ItemsSource = displayTasks.Where(t => t.Status == "InProgress").OrderBy(t => t.DueDate).ToList();
            lstDone.ItemsSource = displayTasks.Where(t => t.Status == "Done").OrderByDescending(t => t.DueDate).ToList();
        }

        // ═══ S2-05: Drag-and-Drop ═══

        private void StartDrag(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox lb && lb.SelectedItem is DailyTask task)
            {
                _draggedTask = task;
                DragDrop.DoDragDrop(lb, task, DragDropEffects.Move);
            }
        }

        private void AllowDropHandler(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void HandleDrop(string targetStatus)
        {
            if (_draggedTask == null) return;

            var task = _db.DailyTasks.Find(_draggedTask.Id);
            if (task != null && task.Status != targetStatus)
            {
                task.Status = targetStatus;
                _db.SaveChanges();
                LoadBoard();
            }
            _draggedTask = null;
        }

        // ═══ Existing Button Handlers ═══

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int taskId)
            {
                var task = _db.DailyTasks.Find(taskId);
                if (task != null)
                {
                    task.Status = "InProgress";
                    _db.SaveChanges();
                    LoadBoard();
                }
            }
        }

        private void BtnDone_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int taskId)
            {
                var task = _db.DailyTasks.Find(taskId);
                if (task != null)
                {
                    task.Status = "Done";
                    _db.SaveChanges();
                    LoadBoard();
                }
            }
        }
    }
}

