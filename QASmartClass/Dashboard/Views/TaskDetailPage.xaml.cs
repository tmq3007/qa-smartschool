using QASmartClass.Data;
using QASmartClass.Services;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QASmartClass.Dashboard.Views
{
    public partial class TaskDetailPage : Page
    {
        private readonly AppDbContext _db;
        private readonly TaskManagementService _taskService;
        private readonly int _taskId;

        public TaskDetailPage(AppDbContext db, int taskId)
        {
            InitializeComponent();
            _db = db;
            _taskService = new TaskManagementService(_db);
            _taskId = taskId;

            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadTaskData();
        }

        private void LoadTaskData()
        {
            try
            {
                var task = _db.TaskItems.Find(_taskId);
                if (task == null)
                {
                    MessageBox.Show("Không tìm thấy công việc.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                TxtTitle.Text = task.Title;
                TxtDescription.Text = string.IsNullOrWhiteSpace(task.Description) ? "Không có mô tả chi tiết." : task.Description;
                TxtPriority.Text = $"Mức độ: {task.Priority}";
                TxtStatus.Text = $"Trạng thái: {task.Status}";
                TxtDeadline.Text = $"Hạn chót: {task.Deadline:dd/MM/yyyy}";
                TxtAssignedBy.Text = task.AssignedBy;
                TxtAssignedTo.Text = task.AssignedTo;

                LoadComments();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi tải chi ti?t Task {TaskId}", _taskId);
            }
        }

        private void LoadComments()
        {
            try
            {
                var comments = _db.TaskComments.Where(c => c.TaskId == _taskId).OrderBy(c => c.CreatedAt).ToList();
                LvComments.ItemsSource = comments;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi tải bình luận cho Task {TaskId}", _taskId);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            // Trong th?c t?, g?i NavigationService.GoBack()
            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        private void BtnSendComment_Click(object sender, RoutedEventArgs e)
        {
            var content = TxtNewComment.Text.Trim();
            if (string.IsNullOrEmpty(content)) return;

            string currentUser = "GV_NguyenVanA"; // Mock

            var newComment = _taskService.AddComment(_taskId, currentUser, content);
            if (newComment != null)
            {
                TxtNewComment.Text = "";
                LoadComments();
                
                // Scroll to bottom
                if (LvComments.Items.Count > 0)
                {
                    LvComments.ScrollIntoView(LvComments.Items[LvComments.Items.Count - 1]);
                }
            }
            else
            {
                MessageBox.Show("Không thể gửi bình luận.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

