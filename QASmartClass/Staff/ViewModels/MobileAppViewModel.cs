using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QASmartClass.Data;
using QASmartClass.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using QASmartClass.Staff.Services;

namespace QASmartClass.Staff.ViewModels
{
    public partial class MobileAppViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<MobileTokenDisplay> _devices = new();

        public MobileAppViewModel()
        {
        }

        public async Task InitializeAsync()
        {
            await LoadDevicesAsync();
        }

        [RelayCommand]
        private async Task LoadDevicesAsync()
        {
            try
            {
                using var db = StaffDbFactory.Create();
                var mobileApiService = new MobileApiService(db);
                var data = await mobileApiService.GetAllDevicesAsync();
                var mapped = data.Select(d => new MobileTokenDisplay
                {
                    Id = d.Id,
                    UserId = d.UserId,
                    Role = d.Role,
                    DeviceName = d.DeviceName,
                    Platform = d.Platform,
                    LastActive = d.LastActive,
                    IsActive = d.IsActive,
                    ApprovalStatus = d.ApprovalStatus ?? "Approved"
                }).ToList();

                Devices = new ObservableCollection<MobileTokenDisplay>(mapped);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[MobileApp] Load failed");
            }
        }

        [RelayCommand]
        private async Task ApproveAsync(int tokenId)
        {
            try
            {
                using var db = StaffDbFactory.Create();
                var mobileApiService = new MobileApiService(db);
                bool success = await mobileApiService.ApproveDeviceAsync(tokenId);
                if (success)
                {
                    await AppServices.UIService.ShowInfoAsync("Đã phê duyệt thiết bị thành công.", "Thành công");
                    await LoadDevicesAsync();
                }
                else
                {
                    await AppServices.UIService.ShowInfoAsync("Không thể phê duyệt thiết bị.", "Lỗi");
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[MobileApp] Approve failed");
            }
        }

        [RelayCommand]
        private async Task RejectAsync(int tokenId)
        {
            try
            {
                using var db = StaffDbFactory.Create();
                var mobileApiService = new MobileApiService(db);
                bool success = await mobileApiService.RejectDeviceAsync(tokenId);
                if (success)
                {
                    await AppServices.UIService.ShowInfoAsync("Đã từ chối kết nối thiết bị.", "Thành công");
                    await LoadDevicesAsync();
                }
                else
                {
                    await AppServices.UIService.ShowInfoAsync("Không thể từ chối kết nối thiết bị.", "Lỗi");
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[MobileApp] Reject failed");
            }
        }

        [RelayCommand]
        private async Task RevokeAsync(int tokenId)
        {
            bool confirmRevoke = await AppServices.UIService.ShowConfirmAsync("Bạn có chắc muốn hủy kết nối thiết bị này? Phụ huynh/Học sinh sẽ phải đăng nhập lại.", "Xác nhận", true);
            
            if (confirmRevoke)
            {
                try
                {
                    using var db = StaffDbFactory.Create();
                    var mobileApiService = new MobileApiService(db);
                    bool success = await mobileApiService.DeactivateDeviceAsync(tokenId);
                    if (success)
                      {
                        await AppServices.UIService.ShowInfoAsync("Đã hủy kết nối thiết bị thành công.", "Thành công");
                        await LoadDevicesAsync();
                    }
                    else
                    {
                        await AppServices.UIService.ShowInfoAsync("Không thể hủy kết nối thiết bị. Thiết bị không tồn tại hoặc đã bị vô hiệu hóa trước đó.", "Lỗi");
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "[MobileApp] Revoke failed");
                }
            }
        }
    }

    public class MobileTokenDisplay
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public DateTime LastActive { get; set; }
        public bool IsActive { get; set; }
        public string ApprovalStatus { get; set; } = "Approved";

        public string FriendlyRole => Role switch
        {
            "Parent" => "Phụ huynh",
            "Student" => "Học sinh",
            _ => Role
        };

        public string FriendlyPlatform => Platform.ToUpper() switch
        {
            "IOS" => "iOS",
            "ANDROID" => "Android",
            _ => Platform
        };

        public string FriendlyStatus => ApprovalStatus switch
        {
            "Pending" => "Chờ phê duyệt",
            "Rejected" => "Đã từ chối",
            "Approved" => IsActive ? "Đang hoạt động" : "Đã hủy kết nối",
            _ => IsActive ? "Đang hoạt động" : "Đã hủy kết nối"
        };

        public string StatusColor => ApprovalStatus switch
        {
            "Pending" => "#D97706",  // Amber (Yellow-orange)
            "Rejected" => "#DC2626", // Red
            "Approved" => IsActive ? "#059669" : "#4B5563", // Green or Gray
            _ => IsActive ? "#059669" : "#4B5563"
        };
    }
}
