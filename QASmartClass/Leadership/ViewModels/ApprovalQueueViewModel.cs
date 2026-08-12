using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;

namespace QASmartClass.Leadership.ViewModels
{
    public partial class ApprovalQueueViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Lesson> _pendingLessons = new();

        [ObservableProperty]
        private Lesson? _selectedLesson;

        [ObservableProperty]
        private string _reviewNotes = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _selectedSubject = "Tất cả";

        public ObservableCollection<string> Subjects { get; } = new()
        {
            "Tất cả", "Toán", "Ngữ văn", "Tiếng Anh", "Vật lý", "Hóa học", "Sinh học", "Lịch sử", "Địa lý", "Tin học"
        };

        private List<Lesson> _allPendingLessons = new();

        public ApprovalQueueViewModel()
        {
        }

        [RelayCommand]
        private async Task LoadPendingLessonsAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var lessons = await db.Lessons
                    .Where(l => l.Status == "PendingApproval")
                    .OrderByDescending(l => l.UpdatedAt)
                    .ToListAsync();
                
                _allPendingLessons = lessons;
                ApplyFilter();
                StatusMessage = $"Tìm thấy {_allPendingLessons.Count} bài giảng chờ duyệt.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi tải dữ liệu: {ex.Message}";
            }
        }

        private void ApplyFilter()
        {
            var filtered = _allPendingLessons.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string search = SearchText.ToLower();
                filtered = filtered.Where(l => 
                    (l.Title != null && l.Title.ToLower().Contains(search)) || 
                    (l.TeacherName != null && l.TeacherName.ToLower().Contains(search)) ||
                    (l.ClassName != null && l.ClassName.ToLower().Contains(search))
                );
            }

            if (SelectedSubject != "Tất cả" && !string.IsNullOrEmpty(SelectedSubject))
            {
                filtered = filtered.Where(l => l.Subject == SelectedSubject);
            }

            PendingLessons = new ObservableCollection<Lesson>(filtered.ToList());
        }

        partial void OnSearchTextChanged(string value) => ApplyFilter();
        partial void OnSelectedSubjectChanged(string value) => ApplyFilter();

        [RelayCommand]
        private async Task ApproveLessonAsync()
        {
            if (SelectedLesson == null) return;

            try
            {
                using var db = new AppDbContext();
                var lessonToUpdate = await db.Lessons.FindAsync(SelectedLesson.Id);
                
                if (lessonToUpdate != null)
                {
                    lessonToUpdate.Status = "Approved";
                    lessonToUpdate.ReviewNotes = ReviewNotes;
                    lessonToUpdate.UpdatedAt = DateTime.Now;

                    // Ghi nhật ký kiểm toán hệ thống
                    db.AuditLogs.Add(new AuditLog
                    {
                        Action = "Lesson_Approved",
                        ActorName = Environment.MachineName,
                        Details = $"Đã phê duyệt bài giảng '{lessonToUpdate.Title}' của GV '{lessonToUpdate.TeacherName}'. Ghi chú: {ReviewNotes}",
                        Timestamp = DateTime.Now
                    });

                    await db.SaveChangesAsync();

                    _allPendingLessons.RemoveAll(l => l.Id == SelectedLesson.Id);
                    ApplyFilter();

                    SelectedLesson = null;
                    ReviewNotes = string.Empty;
                    StatusMessage = "Đã phê duyệt bài giảng thành công!";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task RejectLessonAsync()
        {
            if (SelectedLesson == null) return;
            
            if (string.IsNullOrWhiteSpace(ReviewNotes))
            {
                StatusMessage = "Vui lòng nhập lý do từ chối vào ô Ghi chú.";
                return;
            }

            try
            {
                using var db = new AppDbContext();
                var lessonToUpdate = await db.Lessons.FindAsync(SelectedLesson.Id);
                
                if (lessonToUpdate != null)
                {
                    lessonToUpdate.Status = "Draft"; // Trở về cho GV sửa
                    lessonToUpdate.ReviewNotes = ReviewNotes;
                    lessonToUpdate.UpdatedAt = DateTime.Now;

                    // Ghi nhật ký kiểm toán hệ thống
                    db.AuditLogs.Add(new AuditLog
                    {
                        Action = "Lesson_Rejected",
                        ActorName = Environment.MachineName,
                        Details = $"Từ chối bài giảng '{lessonToUpdate.Title}' của GV '{lessonToUpdate.TeacherName}'. Lý do: {ReviewNotes}",
                        Timestamp = DateTime.Now
                    });

                    await db.SaveChangesAsync();

                    _allPendingLessons.RemoveAll(l => l.Id == SelectedLesson.Id);
                    ApplyFilter();

                    SelectedLesson = null;
                    ReviewNotes = string.Empty;
                    StatusMessage = "Đã từ chối bài giảng và trả về nháp để GV sửa đổi.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi: {ex.Message}";
            }
        }
    }
}
