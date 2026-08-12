using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Services;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class ReadingGroupViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        [ObservableProperty] private string _newGroupName = string.Empty;
        [ObservableProperty] private string _newGroupDescription = string.Empty;
        [ObservableProperty] private string _inviteCodeInput = string.Empty;
        [ObservableProperty] private GroupDto? _selectedGroup;

        public ObservableCollection<GroupDto> MyGroups { get; } = new();
        public ObservableCollection<GroupMemberDto> GroupLeaderboard { get; } = new();

        public ICommand LoadGroupsCommand { get; }
        public ICommand CreateGroupCommand { get; }
        public ICommand JoinGroupCommand { get; }

        public ReadingGroupViewModel(ApiService apiService)
        {
            _apiService = apiService;
            LoadGroupsCommand = new AsyncRelayCommand(LoadGroupsAsync);
            CreateGroupCommand = new AsyncRelayCommand(CreateGroupAsync);
            JoinGroupCommand = new AsyncRelayCommand(JoinGroupAsync);

            _ = LoadGroupsAsync();
        }

        public async Task LoadGroupsAsync()
        {
            try
            {
                var groups = await _apiService.GetAsync<List<GroupDto>>("/ReadingGroups/my-groups");
                if (groups != null)
                {
                    MyGroups.Clear();
                    foreach (var g in groups)
                    {
                        MyGroups.Add(g);
                    }
                    if (MyGroups.Count > 0 && SelectedGroup == null)
                    {
                        SelectedGroup = MyGroups[0];
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi nạp danh sách nhóm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        partial void OnSelectedGroupChanged(GroupDto? value)
        {
            GroupLeaderboard.Clear();
            if (value != null && value.Leaderboard != null)
            {
                foreach (var member in value.Leaderboard)
                {
                    GroupLeaderboard.Add(member);
                }
            }
        }

        private async Task CreateGroupAsync()
        {
            if (string.IsNullOrWhiteSpace(NewGroupName))
            {
                MessageBox.Show("Vui lòng nhập tên nhóm!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var response = await _apiService.PostAsync<object, object>("/ReadingGroups/create", new
                {
                    Name = NewGroupName,
                    Description = NewGroupDescription
                });

                MessageBox.Show("🎉 Tạo nhóm đọc sách mới thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                NewGroupName = string.Empty;
                NewGroupDescription = string.Empty;
                await LoadGroupsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo nhóm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task JoinGroupAsync()
        {
            if (string.IsNullOrWhiteSpace(InviteCodeInput) || InviteCodeInput.Length != 8)
            {
                MessageBox.Show("Mã mời phải gồm đúng 8 ký tự!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var response = await _apiService.PostAsync<object, object>("/ReadingGroups/join", new
                {
                    InviteCode = InviteCodeInput
                });

                MessageBox.Show("🎉 Gia nhập nhóm mới thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                InviteCodeInput = string.Empty;
                await LoadGroupsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi gia nhập nhóm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class GroupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string InviteCode { get; set; } = string.Empty;
        public string CreatedBySsoId { get; set; } = string.Empty;
        public List<GroupMemberDto> Leaderboard { get; set; } = new();
    }

    public class GroupMemberDto
    {
        public string FullName { get; set; } = string.Empty;
        public string SsoId { get; set; } = string.Empty;
        public int XpPoints { get; set; }
        public string ReadingLevel { get; set; } = string.Empty;
    }
}
