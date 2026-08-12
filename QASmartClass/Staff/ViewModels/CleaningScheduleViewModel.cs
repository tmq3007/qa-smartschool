using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace QASmartClass.Staff.ViewModels
{
    public partial class CleaningScheduleViewModel : ObservableObject, IDisposable
    {
        private readonly System.Windows.Threading.DispatcherTimer? _autoUpdateTimer;
        public System.Windows.Threading.DispatcherTimer? AutoUpdateTimer => _autoUpdateTimer;

        [ObservableProperty] private ObservableCollection<CleaningTaskDisplay> _tasks = new();
        [ObservableProperty] private bool _isProcessing;
        [ObservableProperty] private string _area = string.Empty;
        [ObservableProperty] private ObservableCollection<TeacherProfile> _janitorProfiles = new();
        [ObservableProperty] private TeacherProfile? _selectedJanitor;
        [ObservableProperty] private string _note = string.Empty;
        [ObservableProperty] private DateTime? _taskDate = DateTime.Today;
        [ObservableProperty] private string _selectedShift = "Morning";
        [ObservableProperty] private string _progressText = "Tiến độ vệ sinh hôm nay: Chưa có phân công";

        public string JanitorName
        {
            get => SelectedJanitor?.FullName ?? string.Empty;
            set
            {
                var match = JanitorProfiles.FirstOrDefault(j => j.FullName == value);
                if (match != null)
                {
                    SelectedJanitor = match;
                }
                else
                {
                    var newJanitor = new TeacherProfile { TeacherCode = "LC_TEMP", FullName = value, Role = "LaoCong", IsActive = true };
                    JanitorProfiles.Add(newJanitor);
                    SelectedJanitor = newJanitor;
                }
            }
        }

        public CleaningScheduleViewModel()
        {
            if (System.Windows.Application.Current != null)
            {
                _autoUpdateTimer = new System.Windows.Threading.DispatcherTimer();
                _autoUpdateTimer.Interval = TimeSpan.FromSeconds(60);
                _autoUpdateTimer.Tick += (s, e) => { _ = LoadTasksAsync(); };
                _autoUpdateTimer.Start();
            }
        }

        public async Task InitializeAsync()
        {
            await LoadJanitorsAsync();
            await LoadTasksAsync();
        }

        private async Task LoadJanitorsAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var cleanStaff = await db.TeacherProfiles
                    .Where(t => t.Role == "LaoCong" && t.IsActive)
                    .ToListAsync();
                    
                if (!cleanStaff.Any())
                {
                    cleanStaff.Add(new TeacherProfile { TeacherCode = "LC01", FullName = "Cô Nguyễn Thị Lao Công", Role = "LaoCong", IsActive = true });
                    cleanStaff.Add(new TeacherProfile { TeacherCode = "LC02", FullName = "Chú Trần Văn Lao Công", Role = "LaoCong", IsActive = true });
                }
                JanitorProfiles = new ObservableCollection<TeacherProfile>(cleanStaff);
                SelectedJanitor = JanitorProfiles.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[CleaningSchedule] Load janitors failed");
                var cleanStaff = new System.Collections.Generic.List<TeacherProfile>
                {
                    new TeacherProfile { TeacherCode = "LC01", FullName = "Cô Nguyễn Thị Lao Công", Role = "LaoCong", IsActive = true },
                    new TeacherProfile { TeacherCode = "LC02", FullName = "Chú Trần Văn Lao Công", Role = "LaoCong", IsActive = true }
                };
                JanitorProfiles = new ObservableCollection<TeacherProfile>(cleanStaff);
                SelectedJanitor = JanitorProfiles.FirstOrDefault();
            }
        }

        partial void OnTaskDateChanged(DateTime? value)
        {
            _ = LoadTasksAsync();
        }

        partial void OnSelectedJanitorChanged(TeacherProfile? value)
        {
            OnPropertyChanged(nameof(JanitorName));
        }

        [RelayCommand]
        private async Task LoadTasksAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var janitorService = new JanitorService(db);
                var date = TaskDate ?? DateTime.Today;
                var data = await janitorService.GetTasksByDateAsync(date);
                var mapped = data.Select(t => new CleaningTaskDisplay
                {
                    Id = t.Id,
                    Area = t.Area,
                    TaskDate = t.TaskDate,
                    Shift = t.Shift,
                    JanitorName = t.JanitorName,
                    Status = t.Status,
                    Note = t.Note
                });
                Tasks = new ObservableCollection<CleaningTaskDisplay>(mapped);

                // Tính toán tiến độ vệ sinh trong ngày
                var allTasksToday = await db.CleaningTasks.Where(t => t.TaskDate.Date == date.Date).ToListAsync();
                int total = allTasksToday.Count;
                if (total > 0)
                {
                    int done = allTasksToday.Count(t => t.Status == "Done");
                    int pct = (done * 100) / total;
                    ProgressText = $"Tiến độ vệ sinh ngày {date:dd/MM}: {done}/{total} ca hoàn thành ({pct}%)";
                }
                else
                {
                    ProgressText = $"Chưa có phân công ca trực vệ sinh ngày {date:dd/MM}.";
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[CleaningSchedule] Load failed");
            }
        }

        [RelayCommand]
        private async Task AddAsync()
        {
            if (IsProcessing) return;
            IsProcessing = true;
            try
            {
                if (string.IsNullOrWhiteSpace(Area) || SelectedJanitor == null)
                {
                    await AppServices.UIService.ShowInfoAsync("Vui lòng nhập khu vực và người phụ trách.", "Lỗi");
                    return;
                }

                var task = new CleaningTask
                {
                    Area = Area,
                    TaskDate = TaskDate ?? DateTime.Today,
                    Shift = SelectedShift,
                    JanitorName = SelectedJanitor.FullName,
                    JanitorCode = SelectedJanitor.TeacherCode,
                    Note = Note,
                    Status = "Pending"
                };

                using var db = new AppDbContext();
                db.CleaningTasks.Add(task);
                await db.SaveChangesAsync();

                // Ghi nhật ký kiểm toán (Audit Log)
                string actor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "StaffUser";
                QASmartClass.Services.AuditHelper.Log(db, "Add_CleaningTask", actor, $"Assigned cleaning schedule to {SelectedJanitor.FullName} ({SelectedJanitor.TeacherCode}) at area {Area} ({SelectedShift})");
                
                Area = string.Empty;
                Note = string.Empty;
                
                await LoadTasksAsync();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[CleaningSchedule] Add failed");
                await AppServices.UIService.ShowInfoAsync("Lỗi khi thêm phân công.", "Lỗi");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private async Task DoneAsync(CleaningTaskDisplay task)
        {
            if (task == null)
            {
                await AppServices.UIService.ShowInfoAsync("Vui lòng chọn một nhiệm vụ để cập nhật.", "Lỗi");
                return;
            }

            if (task.Status != "Done")
            {
                try
                {
                    using var db = new AppDbContext();
                    var janitorService = new JanitorService(db);
                    if (await janitorService.UpdateTaskStatusAsync(task.Id, "Done"))
                    {
                        // Ghi nhật ký kiểm toán (Audit Log)
                        string actor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "StaffUser";
                        QASmartClass.Services.AuditHelper.Log(db, "Complete_CleaningTask", actor, $"Completed cleaning task ID {task.Id} for area {task.Area} (assigned to {task.JanitorName})");

                        await AppServices.UIService.ShowInfoAsync("Đã đánh dấu hoàn thành!", "Thành công");
                        await LoadTasksAsync();
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "[CleaningSchedule] Done failed");
                }
            }
            else
            {
                await AppServices.UIService.ShowInfoAsync("Nhiệm vụ này đã hoàn thành.", "Thông báo");
            }
        }

        public void Dispose()
        {
            _autoUpdateTimer?.Stop();
        }
    }

    public class CleaningTaskDisplay
    {
        public int Id { get; set; }
        public string Area { get; set; } = string.Empty;
        public DateTime TaskDate { get; set; }
        public string Shift { get; set; } = string.Empty;
        public string JanitorName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;

        public string FriendlyShift => Shift switch
        {
            "Morning" => "Ca Sáng",
            "Afternoon" => "Ca Chiều",
            "Evening" => "Ca Tối",
            _ => Shift
        };

        public string FriendlyStatus => Status switch
        {
            "Pending" => "Chờ thực hiện",
            "InProgress" => "Đang thực hiện",
            "Done" => "Đã hoàn thành",
            _ => Status
        };
    }
}
