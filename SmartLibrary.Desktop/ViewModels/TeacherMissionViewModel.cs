using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Services;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class TeacherMissionViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private string _selectedClass = "";

        [ObservableProperty]
        private BookSimpleDto? _selectedBook;

        [ObservableProperty]
        private string _missionTitle = "";

        [ObservableProperty]
        private string _instructions = "";

        [ObservableProperty]
        private DateTime _deadline = DateTime.Now.AddDays(7);

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private TeacherMissionItemDto? _selectedMission;

        [ObservableProperty]
        private string _studentSearchKeyword = "";

        private readonly System.Collections.Generic.List<StudentMissionProgressDto> _allStudentProgresses = new();

        partial void OnStudentSearchKeywordChanged(string value)
        {
            ApplyStudentProgressFilter();
        }

        private void ApplyStudentProgressFilter()
        {
            StudentProgresses.Clear();
            var query = StudentSearchKeyword?.Trim().ToLower() ?? "";
            var filtered = _allStudentProgresses.Where(p =>
                string.IsNullOrEmpty(query) ||
                p.StudentName.ToLower().Contains(query) ||
                p.StudentSsoId.ToLower().Contains(query)
            );
            foreach (var p in filtered)
            {
                StudentProgresses.Add(p);
            }
        }

        public ObservableCollection<string> AvailableClasses { get; } = new() { "10A1", "10A2", "11A1", "11B2", "12C1" };
        public ObservableCollection<BookSimpleDto> Books { get; } = new();
        public ObservableCollection<TeacherMissionItemDto> AssignedMissions { get; } = new();
        public ObservableCollection<StudentMissionProgressDto> StudentProgresses { get; } = new();

        public ICommand LoadMissionsCommand { get; }
        public ICommand AssignMissionCommand { get; }
        public ICommand LoadProgressCommand { get; }

        public TeacherMissionViewModel(ApiService apiService)
        {
            _apiService = apiService;

            LoadMissionsCommand = new AsyncRelayCommand(LoadMissionsAsync);
            AssignMissionCommand = new AsyncRelayCommand(AssignMissionAsync);
            LoadProgressCommand = new AsyncRelayCommand(LoadProgressAsync);

            // Tự động gán lớp theo SSO ID của giáo viên giống như TeacherDashboard
            var ssoId = AuthService.CurrentUserSsoId ?? "GV001";
            AvailableClasses.Clear();
            if (ssoId == "GV001")
            {
                SelectedClass = "10A1";
                AvailableClasses.Add("10A1");
                AvailableClasses.Add("10A2");
            }
            else if (ssoId == "GV002")
            {
                SelectedClass = "11B2";
                AvailableClasses.Add("11B2");
                AvailableClasses.Add("11A1");
            }
            else if (ssoId == "GV003")
            {
                SelectedClass = "10A2";
                AvailableClasses.Add("10A2");
            }
            else if (ssoId == "GV004")
            {
                SelectedClass = "11A1";
                AvailableClasses.Add("11A1");
            }
            else if (ssoId == "GV005")
            {
                SelectedClass = "12C1";
                AvailableClasses.Add("12C1");
            }
            else
            {
                SelectedClass = "10A1";
                AvailableClasses.Add("10A1");
                AvailableClasses.Add("11B2");
                AvailableClasses.Add("10A2");
                AvailableClasses.Add("11A1");
                AvailableClasses.Add("12C1");
            }

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadBooksAsync();
            await LoadMissionsAsync();
        }

        private async Task LoadBooksAsync()
        {
            try
            {
                var list = await _apiService.GetAsync<List<BookSimpleDto>>("/Books");
                if (list != null)
                {
                    Books.Clear();
                    foreach (var b in list)
                    {
                        Books.Add(b);
                    }
                    if (Books.Count > 0) SelectedBook = Books[0];
                }
            }
            catch (Exception)
            {
                // Fallback offline books
                Books.Clear();
                Books.Add(new BookSimpleDto { Id = 1, Title = "Tắt Đèn", Author = "Ngô Tất Tố" });
                Books.Add(new BookSimpleDto { Id = 2, Title = "Số Đỏ", Author = "Vũ Trọng Phụng" });
                Books.Add(new BookSimpleDto { Id = 3, Title = "Đất Rừng Phương Nam", Author = "Đoàn Giỏi" });
                Books.Add(new BookSimpleDto { Id = 4, Title = "Đắc Nhân Tâm", Author = "Dale Carnegie" });
                if (Books.Count > 0) SelectedBook = Books[0];
            }
        }

        public async Task LoadMissionsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                await SyncPendingMissionsAsync(_apiService);
                var list = await _apiService.GetAsync<List<TeacherMissionItemDto>>("/Missions/teacher-missions");
                if (list != null)
                {
                    AssignedMissions.Clear();
                    foreach (var m in list)
                    {
                        AssignedMissions.Add(m);
                    }
                }
            }
            catch (Exception)
            {
                // Fallback mock missions
                AssignedMissions.Clear();
                AssignedMissions.Add(new TeacherMissionItemDto 
                { 
                    Id = 1, 
                    Title = "Đọc tác phẩm Tắt Đèn", 
                    ClassId = SelectedClass, 
                    BookTitle = "Tắt Đèn", 
                    Deadline = DateTime.Now.AddDays(3), 
                    CreatedAt = DateTime.Now.AddDays(-2) 
                });
            }
            finally
            {
                IsBusy = false;
            }
        }

        partial void OnSelectedMissionChanged(TeacherMissionItemDto? value)
        {
            StudentSearchKeyword = "";
            if (value != null)
            {
                _ = LoadProgressAsync();
            }
            else
            {
                StudentProgresses.Clear();
                _allStudentProgresses.Clear();
            }
        }

        public async Task LoadProgressAsync()
        {
            if (SelectedMission == null) return;
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                var response = await _apiService.GetAsync<MissionProgressResponseDto>($"/Missions/{SelectedMission.Id}/progress");
                _allStudentProgresses.Clear();
                if (response != null)
                {
                    foreach (var p in response.Students)
                    {
                        _allStudentProgresses.Add(p);
                    }
                }
                ApplyStudentProgressFilter();
            }
            catch (Exception)
            {
                // Fallback mock progress
                _allStudentProgresses.Clear();
                _allStudentProgresses.Add(new StudentMissionProgressDto { Id = 1, StudentName = "Nguyễn Văn An", StudentSsoId = "HS001", IsCompleted = true, QuizScoreText = "5/5", IsQuizPassed = true, CompletedAt = DateTime.Now.AddDays(-1) });
                _allStudentProgresses.Add(new StudentMissionProgressDto { Id = 2, StudentName = "Trần Thị Bình", StudentSsoId = "HS002", IsCompleted = false, QuizScoreText = "Chưa làm", IsQuizPassed = false });
                _allStudentProgresses.Add(new StudentMissionProgressDto { Id = 3, StudentName = "Lê Hoàng Cường", StudentSsoId = "HS003", IsCompleted = true, QuizScoreText = "3/5", IsQuizPassed = false, CompletedAt = DateTime.Now });
                ApplyStudentProgressFilter();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private string GetMissionsFilePath()
        {
            var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SmartLibrary", "data");
            if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
            return System.IO.Path.Combine(dir, "assigned_missions.json");
        }

        private List<AssignMissionRequestDto> LoadMissionsFromFile()
        {
            try
            {
                var path = GetMissionsFilePath();
                if (!System.IO.File.Exists(path)) return new List<AssignMissionRequestDto>();
                var json = System.IO.File.ReadAllText(path);
                return System.Text.Json.JsonSerializer.Deserialize<List<AssignMissionRequestDto>>(json) 
                       ?? new List<AssignMissionRequestDto>();
            }
            catch
            {
                return new List<AssignMissionRequestDto>();
            }
        }

        public static async Task SyncPendingMissionsAsync(ApiService apiService)
        {
            try
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SmartLibrary", "data");
                var filePath = System.IO.Path.Combine(dir, "assigned_missions.json");
                
                if (!System.IO.File.Exists(filePath)) return;

                string json = await System.IO.File.ReadAllTextAsync(filePath);
                if (string.IsNullOrWhiteSpace(json)) return;

                var pendingMissions = System.Text.Json.JsonSerializer.Deserialize<List<AssignMissionRequestDto>>(json);
                if (pendingMissions == null || pendingMissions.Count == 0) return;

                bool allSynced = true;
                var successfullySynced = new List<AssignMissionRequestDto>();
                foreach (var mission in pendingMissions)
                {
                    try
                    {
                        await apiService.PostAsync("/Missions/assign", mission, silent: true);
                        successfullySynced.Add(mission);
                    }
                    catch
                    {
                        allSynced = false;
                    }
                }

                if (allSynced)
                {
                    System.IO.File.Delete(filePath);
                }
                else if (successfullySynced.Count > 0)
                {
                    foreach (var item in successfullySynced)
                    {
                        pendingMissions.Remove(item);
                    }
                    string syncJson = System.Text.Json.JsonSerializer.Serialize(pendingMissions);
                    await System.IO.File.WriteAllTextAsync(filePath, syncJson);
                }
            }
            catch { }
        }

        private async Task AssignMissionAsync()
        {
            if (string.IsNullOrWhiteSpace(MissionTitle))
            {
                MessageBox.Show("❌ Vui lòng nhập tiêu đề nhiệm vụ!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedBook == null)
            {
                MessageBox.Show("❌ Vui lòng chọn cuốn sách cần đọc!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Deadline.Date < DateTime.Today)
            {
                MessageBox.Show("❌ Thời hạn hoàn thành phải ở hiện tại hoặc tương lai!", "Thông tin không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsBusy = true;
            var payload = new AssignMissionRequestDto
            {
                ClassId = SelectedClass,
                BookId = SelectedBook.Id,
                Title = MissionTitle.Trim(),
                Instructions = Instructions.Trim(),
                Deadline = Deadline
            };

            try
            {
                await _apiService.PostAsync("/Missions/assign", payload);
                await AuditLogService.WriteLogAsync("Giao nhiệm vụ", 
                    $"GV {AuthService.CurrentUserSsoId ?? "GV"} giao nhiệm vụ '{payload.Title}' cho lớp {payload.ClassId}", true);

                MessageBox.Show("🎉 Giao nhiệm vụ đọc sách thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                // Reset form
                MissionTitle = "";
                Instructions = "";
                Deadline = DateTime.Now.AddDays(7);
            }
            catch (Exception)
            {
                var offlineMissions = LoadMissionsFromFile();
                offlineMissions.Add(payload);
                try
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(offlineMissions);
                    System.IO.File.WriteAllText(GetMissionsFilePath(), json);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ Lỗi lưu file ngoại tuyến: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    IsBusy = false;
                    return;
                }

                await AuditLogService.WriteLogAsync("Giao nhiệm vụ", 
                    $"GV {AuthService.CurrentUserSsoId ?? "GV"} giao nhiệm vụ '{payload.Title}' cho lớp {payload.ClassId} (Ngoại tuyến)", true);

                MessageBox.Show("🎉 Giao nhiệm vụ đọc sách thành công (Chế độ offline)!", "Thành công (Offline)", MessageBoxButton.OK, MessageBoxImage.Information);

                // Reset form
                MissionTitle = "";
                Instructions = "";
                Deadline = DateTime.Now.AddDays(7);
            }

            await LoadMissionsAsync();
            IsBusy = false;
        }
    }

    public class BookSimpleDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        
        public string DisplayText => $"{Title} - {Author}";
    }

    public class TeacherMissionItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ClassId { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public DateTime Deadline { get; set; }
        public DateTime CreatedAt { get; set; }

        public string FormattedDeadline => Deadline.ToString("dd/MM/yyyy");
        public string FormattedCreatedAt => CreatedAt.ToString("dd/MM/yyyy HH:mm");
        public string DisplayText => $"[{ClassId}] {Title} (Hạn: {FormattedDeadline})";
    }

    public class MissionProgressResponseDto
    {
        public int MissionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public string ClassId { get; set; } = string.Empty;
        public DateTime Deadline { get; set; }
        public List<StudentMissionProgressDto> Students { get; set; } = new();
    }

    public class StudentMissionProgressDto
    {
        public int Id { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentSsoId { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public string QuizScoreText { get; set; } = "Chưa làm";
        public bool IsQuizPassed { get; set; }
        public DateTime? CompletedAt { get; set; }

        public string FormattedCompletedAt => CompletedAt.HasValue ? CompletedAt.Value.ToString("dd/MM/yyyy HH:mm") : "N/A";
        public string StatusText => IsCompleted ? "Đã Hoàn Thành" : "Chưa hoàn thành";
        public string StatusColor => IsCompleted ? "#10B981" : "#94A388";
    }

    public class AssignMissionRequestDto
    {
        public string ClassId { get; set; } = string.Empty;
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public DateTime Deadline { get; set; }
    }
}
