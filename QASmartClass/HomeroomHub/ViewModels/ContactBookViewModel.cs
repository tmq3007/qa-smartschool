using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;

namespace QASmartClass.HomeroomHub.ViewModels
{
    public partial class ContactBookViewModel : ObservableObject
    {
        [ObservableProperty]
        private ClassRoster? _homeroomClass;

        [ObservableProperty]
        private ObservableCollection<ContactBookEntry> _entries = new();

        [ObservableProperty]
        private ContactBookEntry? _selectedEntry;

        [ObservableProperty]
        private string _teacherComment = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isExporting;

        public ContactBookViewModel()
        {
            _ = LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            StatusMessage = "Đang tải sổ liên lạc...";
            try
            {
                using var db = new AppDbContext();
                var service = new ContactBookService(db);
                // Giả định: Lấy lớp chủ nhiệm đầu tiên đang hoạt động
                var roster = await db.ClassRosters.FirstOrDefaultAsync(r => r.IsActive);
                if (roster == null)
                {
                    StatusMessage = "Không tìm thấy lớp học nào.";
                    return;
                }

                HomeroomClass = roster;

                // Load or create entries for each student in the roster
                var rosterStudents = await db.ClassRosterStudents
                    .Where(rs => rs.RosterId == roster.Id)
                    .ToListAsync();

                var list = new ObservableCollection<ContactBookEntry>();
                var entriesList = await Task.Run(() =>
                {
                    using var bgDb = new AppDbContext();
                    var bgService = new ContactBookService(bgDb);
                    var temp = new List<ContactBookEntry>();
                    foreach (var rs in rosterStudents)
                    {
                        var entry = bgService.GetOrCalculateEntry(rs.StudentId, roster.SchoolYear, roster.Semester);
                        temp.Add(entry);
                    }
                    return temp;
                });

                foreach (var entry in entriesList)
                {
                    list.Add(entry);
                }

                Entries = list;
                if (Entries.Any())
                {
                    SelectedEntry = Entries.First();
                }

                StatusMessage = $"Đã tải {Entries.Count} sổ liên lạc lớp {roster.ClassName}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi: {ex.Message}";
            }
        }

        partial void OnSelectedEntryChanged(ContactBookEntry? value)
        {
            if (value != null)
            {
                TeacherComment = value.TeacherComment;
            }
            else
            {
                TeacherComment = string.Empty;
            }
        }

        [RelayCommand]
        private void SaveComment()
        {
            if (SelectedEntry == null) return;

            try
            {
                using var db = new AppDbContext();
                var service = new ContactBookService(db);
                service.SaveComment(SelectedEntry.Id, TeacherComment);
                SelectedEntry.TeacherComment = TeacherComment;
                SelectedEntry.UpdatedAt = DateTime.Now;
                StatusMessage = "✅ Đã lưu nhận xét giáo viên!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi khi lưu nhận xét: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task ExportPdf()
        {
            if (SelectedEntry == null) return;

            IsExporting = true;
            try
            {
                StatusMessage = "Đang xuất sổ liên lạc...";
                string path = string.Empty;
                await Task.Run(() => 
                {
                    using var db = new AppDbContext();
                    var service = new ContactBookService(db);
                    path = service.ExportContactBookPdf(SelectedEntry.StudentId, SelectedEntry.SchoolYear, SelectedEntry.Semester);
                });
                MessageBox.Show($"Đã xuất sổ liên lạc thành công!\nĐường dẫn: {path}", "Xuất PDF", MessageBoxButton.OK, MessageBoxImage.Information);
                StatusMessage = $"✅ Đã xuất PDF thành công: {System.IO.Path.GetFileName(path)}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi khi xuất PDF: {ex.Message}";
                MessageBox.Show($"Lỗi khi xuất PDF: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsExporting = false;
            }
        }

        [RelayCommand]
        private async Task ExportAllPdf()
        {
            if (!Entries.Any()) return;

            IsExporting = true;
            try
            {
                StatusMessage = "Đang xuất hàng loạt sổ liên lạc...";
                await Task.Run(() => 
                {
                    using var db = new AppDbContext();
                    var service = new ContactBookService(db);
                    foreach (var entry in Entries)
                    {
                        service.ExportContactBookPdf(entry.StudentId, entry.SchoolYear, entry.Semester);
                    }
                });
                MessageBox.Show($"Đã xuất hàng loạt {Entries.Count} sổ liên lạc thành công!\nCác file được lưu tại thư mục báo cáo.", "Xuất PDF Hàng Loạt", MessageBoxButton.OK, MessageBoxImage.Information);
                StatusMessage = $"✅ Đã xuất {Entries.Count} PDF thành công.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi khi xuất PDF hàng loạt: {ex.Message}";
                MessageBox.Show($"Lỗi khi xuất PDF hàng loạt: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsExporting = false;
            }
        }
    }
}

