using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Services;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class UserPermissionsViewModel : ObservableObject, IActiveAwareViewModel
    {
        public partial class UserPermissionDto : ObservableObject
        {
            public string SsoId { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Department { get; set; } = string.Empty;
            
            [ObservableProperty] private string _role = "Librarian";
            
            // Available roles for ComboBox
            public string[] Roles => new[] { "Admin", "Librarian", "Teacher", "Student" };
        }

        private readonly ApiService _apiService;
        private readonly ObservableCollection<UserPermissionDto> _allUsers = new();

        public ObservableCollection<UserPermissionDto> Users { get; } = new();
        public ObservableCollection<string> Departments { get; } = new() { "Tất cả", "Tổ Tự Nhiên", "Tổ Xã Hội", "Ban Giám Hiệu", "Hành Chính" };

        [ObservableProperty] private string _searchKeyword = "";
        [ObservableProperty] private string _selectedDepartment = "Tất cả";
        [ObservableProperty] private string _statusMessage = "";
        [ObservableProperty] private bool _isBusy;

        public ICommand SavePermissionsCommand { get; }

        public UserPermissionsViewModel(ApiService apiService)
        {
            _apiService = apiService;
            SavePermissionsCommand = new AsyncRelayCommand(SavePermissionsAsync);
            
            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SearchKeyword) || e.PropertyName == nameof(SelectedDepartment))
                {
                    ApplyFilters();
                }
            };
        }

        public void Activate()
        {
            LoadMockData();
            ApplyFilters();
        }

        private void LoadMockData()
        {
            _allUsers.Clear();
            _allUsers.Add(new UserPermissionDto { SsoId = "AD001", FullName = "Hoàng Quản Trị", Department = "Ban Giám Hiệu", Role = "Admin" });
            _allUsers.Add(new UserPermissionDto { SsoId = "TT001", FullName = "Võ Minh Em", Department = "Hành Chính", Role = "Librarian" });
            _allUsers.Add(new UserPermissionDto { SsoId = "GV001", FullName = "Phạm Thị Dung", Department = "Tổ Tự Nhiên", Role = "Teacher" });
            _allUsers.Add(new UserPermissionDto { SsoId = "GV002", FullName = "Trần Thị Lan", Department = "Tổ Xã Hội", Role = "Teacher" });
            _allUsers.Add(new UserPermissionDto { SsoId = "TT002", FullName = "Lê Thị Thu Hà", Department = "Hành Chính", Role = "Librarian" });
        }

        private void ApplyFilters()
        {
            Users.Clear();
            var query = _allUsers.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                string kw = SmartLibrary.Desktop.Helpers.InputHelper.RemoveDiacritics(SearchKeyword.Trim()).ToLower();
                query = query.Where(u => 
                    (u.FullName != null && SmartLibrary.Desktop.Helpers.InputHelper.RemoveDiacritics(u.FullName).ToLower().Contains(kw)) || 
                    (u.SsoId != null && SmartLibrary.Desktop.Helpers.InputHelper.RemoveDiacritics(u.SsoId).ToLower().Contains(kw)));
            }
            
            if (SelectedDepartment != "Tất cả")
            {
                query = query.Where(u => u.Department == SelectedDepartment);
            }
            
            foreach (var user in query)
            {
                Users.Add(user);
            }
        }

        private async Task SavePermissionsAsync()
        {
            if (IsBusy) return;

            // Kiểm tra tự hạ quyền của chính mình
            var myRecord = _allUsers.FirstOrDefault(u => u.SsoId == AuthService.CurrentUserSsoId);
            if (myRecord != null && myRecord.Role != "Admin")
            {
                var confirm = System.Windows.MessageBox.Show(
                    "⚠️ CẢNH BÁO NGUY HIỂM: Bạn đang tự thay đổi quyền hạn của chính mình từ Admin sang vai trò khác. Hành động này sẽ khiến bạn mất quyền truy cập trang quản trị ngay lập tức!\n\nBạn vẫn muốn tiếp tục chứ?",
                    "Xác nhận thay đổi quyền hạn bản thân",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);
                if (confirm != System.Windows.MessageBoxResult.Yes)
                {
                    myRecord.Role = "Admin";
                    ApplyFilters();
                    StatusMessage = "Đã hủy bỏ thay đổi quyền hạn bản thân.";
                    return;
                }
            }

            IsBusy = true;
            StatusMessage = "Đang lưu thay đổi cấu hình phân quyền...";
            try
            {
                await Task.Delay(1000);
                
                // Trong mockup, ghi log hành động phân quyền
                string details = string.Join(", ", _allUsers.Select(u => $"{u.SsoId}:{u.Role}"));
                await AuditLogService.WriteLogAsync("Phân quyền", $"Đã cập nhật phân quyền nhân sự nhà trường: {details}", true);
                
                StatusMessage = "✅ Đã lưu cấu hình phân quyền nhân sự thành công!";
                System.Media.SystemSounds.Asterisk.Play();
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
