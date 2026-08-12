using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;

namespace QASmartClass.TeacherHub.ViewModels
{
    public partial class TeacherAssignmentsViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ClassRoster> _classes = new();

        private ClassRoster? _selectedClass;
        public ClassRoster? SelectedClass
        {
            get => _selectedClass;
            set
            {
                if (SetProperty(ref _selectedClass, value))
                {
                    _ = LoadAssignmentsAsync();
                }
            }
        }

        [ObservableProperty]
        private ObservableCollection<Assignment> _assignments = new();

        private Assignment? _selectedAssignment;
        public Assignment? SelectedAssignment
        {
            get => _selectedAssignment;
            set
            {
                if (SetProperty(ref _selectedAssignment, value))
                {
                    _ = LoadSubmissionsAsync();
                }
            }
        }

        [ObservableProperty]
        private ObservableCollection<FileTransferRecord> _submissions = new();

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        // New Assignment Form
        [ObservableProperty]
        private string _newTitle = string.Empty;

        [ObservableProperty]
        private string _newDescription = string.Empty;

        [ObservableProperty]
        private DateTime _newDeadline = DateTime.Today.AddDays(7);

        public TeacherAssignmentsViewModel()
        {
            _ = LoadClassesAsync();
        }

        private async Task LoadClassesAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var rosters = await db.ClassRosters.Where(r => r.IsActive).ToListAsync();
                Classes = new ObservableCollection<ClassRoster>(rosters);
                SelectedClass = Classes.FirstOrDefault();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi tải lớp học: {ex.Message}";
            }
        }

        private async Task LoadAssignmentsAsync()
        {
            if (SelectedClass == null)
            {
                Assignments.Clear();
                return;
            }

            try
            {
                using var db = new AppDbContext();
                var list = await db.Assignments
                    .Where(a => a.RosterId == SelectedClass.Id)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();
                Assignments = new ObservableCollection<Assignment>(list);
                SelectedAssignment = null;
                Submissions.Clear();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi tải bài tập: {ex.Message}";
            }
        }

        private async Task LoadSubmissionsAsync()
        {
            if (SelectedAssignment == null)
            {
                Submissions.Clear();
                return;
            }

            try
            {
                using var db = new AppDbContext();
                var list = await db.FileTransfers
                    .Where(f => f.AssignmentId == SelectedAssignment.Id && f.Direction == "StudentToTeacher")
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();
                Submissions = new ObservableCollection<FileTransferRecord>(list);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi tải bài nộp: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task CreateAssignmentAsync()
        {
            if (SelectedClass == null)
            {
                MessageBox.Show("Vui lòng chọn lớp học.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(NewTitle))
            {
                MessageBox.Show("Tiêu đề bài tập không được để trống.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NewDeadline < DateTime.Today)
            {
                MessageBox.Show("Hạn nộp không hợp lệ (trong quá khứ).", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var db = new AppDbContext();
                var assignment = new Assignment
                {
                    Title = NewTitle,
                    Description = NewDescription,
                    Deadline = NewDeadline,
                    RosterId = SelectedClass.Id,
                    CreatedAt = DateTime.Now
                };

                db.Assignments.Add(assignment);
                await db.SaveChangesAsync();

                NewTitle = string.Empty;
                NewDescription = string.Empty;
                NewDeadline = DateTime.Today.AddDays(7);
                
                await LoadAssignmentsAsync();
                StatusMessage = "✔️ Đã tạo bài tập mới!";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo bài tập: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task DeleteAssignmentAsync(Assignment assignment)
        {
            if (assignment == null) return;

            var result = MessageBox.Show($"Bạn có chắc muốn xóa bài tập '{assignment.Title}'?\nCác bài nộp của học sinh có thể sẽ không còn liên kết.", 
                                         "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                         
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using var db = new AppDbContext();
                    db.Assignments.Remove(assignment);
                    await db.SaveChangesAsync();
                    
                    await LoadAssignmentsAsync();
                    StatusMessage = "✔️ Đã xóa bài tập!";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi xóa bài tập: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void OpenSubmissionFile(FileTransferRecord record)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.FileName)) return;

            try
            {
                // Giả lập mở file từ đường dẫn lưu trữ.
                string basePath = QASmartClass.Services.AppPaths.SubmittedFilesDir;
                string filePath = System.IO.Path.Combine(basePath, record.FileName);
                
                if (System.IO.File.Exists(filePath))
                {
                    var ext = System.IO.Path.GetExtension(filePath).ToLower();
                    string[] dangerousExtensions = { ".exe", ".bat", ".cmd", ".ps1", ".vbs", ".js", ".scr", ".lnk", ".sys", ".com" };
                    
                    if (dangerousExtensions.Contains(ext))
                    {
                        var result = MessageBox.Show(
                            $"[CẢNH BÁO BẢO MẬT]\n\nFile nộp bài \"{record.FileName}\" chứa phần mở rộng nguy hiểm ({ext}).\n\nĐể đảm bảo an toàn cho máy tính giáo viên:\n- Nhấp 'Yes' nếu bạn chỉ muốn MỞ THƯ MỤC chứa tệp trong File Explorer (Khuyên dùng).\n- Nhấp 'No' để bỏ qua và không mở file.",
                            "Cảnh báo Bảo mật Hệ thống",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result == MessageBoxResult.Yes)
                        {
                            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                        }
                        return;
                    }

                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("Không tìm thấy file trên hệ thống lưu trữ.", "Lỗi Tệp", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

