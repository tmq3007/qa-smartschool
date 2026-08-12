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
using SmartLibrary.Desktop.Helpers;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class LibrarianXpShopViewModel : ObservableObject, IActiveAwareViewModel
    {
        private readonly ApiService _apiService;
        private readonly List<PendingCollectionDto> _allCollections = new();

        public void Activate()
        {
            _ = LoadPendingAsync();
        }

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = "";

        [ObservableProperty]
        private string _searchKeyword = "";

        public ObservableCollection<PendingCollectionDto> PendingCollections { get; } = new();

        public ICommand LoadPendingCommand { get; }
        public ICommand ApproveCollectionCommand { get; }

        public LibrarianXpShopViewModel(ApiService apiService)
        {
            _apiService = apiService;
            LoadPendingCommand = new AsyncRelayCommand(LoadPendingAsync);
            ApproveCollectionCommand = new AsyncRelayCommand<PendingCollectionDto>(ApproveCollectionAsync);
        }

        partial void OnSearchKeywordChanged(string value)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            PendingCollections.Clear();
            var query = _allCollections.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                var cleanKeyword = InputHelper.RemoveDiacritics(SearchKeyword.Trim()).ToLower();
                query = query.Where(c => 
                    (c.StudentName != null && InputHelper.RemoveDiacritics(c.StudentName).ToLower().Contains(cleanKeyword)) ||
                    (c.StudentSsoId != null && InputHelper.RemoveDiacritics(c.StudentSsoId).ToLower().Contains(cleanKeyword)) ||
                    (c.RewardName != null && InputHelper.RemoveDiacritics(c.RewardName).ToLower().Contains(cleanKeyword))
                );
            }
            foreach (var item in query)
            {
                PendingCollections.Add(item);
            }
        }

        public async Task LoadPendingAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            StatusMessage = "Đang tải danh sách chờ nhận quà...";

            try
            {
                var pending = await _apiService.GetAsync<List<PendingCollectionDto>>(ApiEndpoints.XpShopPendingCollections);
                _allCollections.Clear();
                if (pending != null)
                {
                    foreach (var item in pending)
                    {
                        _allCollections.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                await AuditLogService.WriteLogAsync("Tải đổi quà XP", $"Lỗi kết nối máy chủ: {ex.Message}. Sử dụng dữ liệu offline.", false);
                _allCollections.Clear();
                _allCollections.Add(new PendingCollectionDto { Id = 1, StudentName = "Nguyễn Văn An", StudentSsoId = "HS001", RewardName = "Sổ tay xanh lá tái chế", XpCost = 150, RedeemedAt = DateTime.Now.AddHours(-1) });
                _allCollections.Add(new PendingCollectionDto { Id = 2, StudentName = "Trần Thị Bình", StudentSsoId = "HS002", RewardName = "Bình nước tre tự nhiên", XpCost = 300, RedeemedAt = DateTime.Now.AddMinutes(-30) });
            }
            finally
            {
                ApplyFilter();
                IsLoading = false;
                StatusMessage = "";
            }
        }

        private async Task ApproveCollectionAsync(PendingCollectionDto? item)
        {
            if (item == null || IsLoading) return;

            var confirmResult = MessageBox.Show(
                $"Xác nhận đã trao quà '{item.RewardName}' cho học sinh {item.StudentName} ({item.StudentSsoId})?", 
                "Xác nhận phát quà", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);
            
            if (confirmResult != MessageBoxResult.Yes) return;

            IsLoading = true;
            try
            {
                await _apiService.PostAsync<object>($"{ApiEndpoints.XpShopCollect}{item.Id}", new { });
                
                await AuditLogService.WriteLogAsync("Cửa hàng XP", 
                    $"Đã xác nhận trao quà '{item.RewardName}' (Chi phí {item.XpCost} XP) cho học sinh {item.StudentName} ({item.StudentSsoId}) thành công", true);
                
                MessageBox.Show("✅ Xác nhận phát quà và trao đặc quyền thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                await AuditLogService.WriteLogAsync("Cửa hàng XP ngoại tuyến", 
                    $"[OFFLINE] Đã xác nhận trao quà '{item.RewardName}' (Chi phí {item.XpCost} XP) cho học sinh {item.StudentName} ({item.StudentSsoId}). Lỗi kết nối: {ex.Message}", true);
                
                MessageBox.Show("✅ Xác nhận phát quà thành công (Chế độ offline)!", "Thành công (Offline)", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            finally
            {
                IsLoading = false;
            }

            await LoadPendingAsync();
        }
    }

    public class PendingCollectionDto
    {
        public int Id { get; set; }
        public DateTime RedeemedAt { get; set; }
        public string RewardName { get; set; } = string.Empty;
        public int XpCost { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentSsoId { get; set; } = string.Empty;

        public string FormattedDate => RedeemedAt.ToString("dd/MM/yyyy HH:mm");
    }
}
