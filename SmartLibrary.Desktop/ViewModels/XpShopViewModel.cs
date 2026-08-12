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
    public partial class XpShopViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private int _currentXp;

        [ObservableProperty]
        private string _currentLevel = "";

        [ObservableProperty]
        private int _maxXp = 500;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = "";

        [ObservableProperty] private string _equippedAvatar = "👦";
        [ObservableProperty] private string _equippedBorder = "None";

        public ObservableCollection<XpRewardDto> Rewards { get; } = new();
        public ObservableCollection<RedeemedRewardDto> MyRedemptions { get; } = new();
        public ObservableCollection<XpTransactionDto> XpTransactions { get; } = new();
        public ObservableCollection<string> OwnedAvatars { get; } = new();
        public ObservableCollection<string> OwnedBorders { get; } = new();

        public ICommand LoadDataCommand { get; }
        public ICommand RedeemCommand { get; }
        public ICommand EquipAvatarCommand { get; }
        public ICommand EquipBorderCommand { get; }

        public XpShopViewModel(ApiService apiService)
        {
            _apiService = apiService;
            LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
            RedeemCommand = new AsyncRelayCommand<XpRewardDto>(RedeemRewardAsync);
            EquipAvatarCommand = new AsyncRelayCommand<string>(EquipAvatarAsync);
            EquipBorderCommand = new AsyncRelayCommand<string>(EquipBorderAsync);

            _ = LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            IsLoading = true;
            StatusMessage = "Đang tải dữ liệu cửa hàng...";
            
            // 1. Tải thông tin điểm cá nhân
            var ssoId = AuthService.CurrentUserSsoId ?? "HS001";
            var progress = GamificationService.GetProgress(ssoId);
            CurrentXp = progress.XP;
            CurrentLevel = progress.Level;
            MaxXp = GamificationService.GetMaxXpForLevel(progress.XP);

            // 2. Tải danh mục quà tặng
            try
            {
                var rewards = await _apiService.GetAsync<List<XpRewardDto>>("/XpShop/rewards");
                if (rewards != null)
                {
                    Rewards.Clear();
                    foreach (var r in rewards)
                    {
                        Rewards.Add(r);
                    }
                }
            }
            catch (Exception)
            {
                // Fallback offline / mock dữ liệu nếu mất mạng
                Rewards.Clear();
                Rewards.Add(new XpRewardDto { Id = 1, Name = "Sổ tay xanh lá tái chế", Description = "Sổ tay lò xo bìa giấy kraft bảo vệ môi trường", XpCost = 150, StockCount = 10, RewardType = "Physical" });
                Rewards.Add(new XpRewardDto { Id = 2, Name = "Bình nước tre tự nhiên", Description = "Bình giữ nhiệt vỏ tre cao cấp khắc logo trường", XpCost = 300, StockCount = 5, RewardType = "Physical" });
                Rewards.Add(new XpRewardDto { Id = 3, Name = "Đặc quyền mượn 5 sách cùng lúc", Description = "Tăng giới hạn mượn sách tối đa từ 3 cuốn lên 5 cuốn", XpCost = 100, StockCount = 99, RewardType = "Privileged" });
                Rewards.Add(new XpRewardDto { Id = 4, Name = "Mượn thêm 3 ngày", Description = "Đặc quyền gia hạn hạn trả thêm 3 ngày cho lượt mượn hiện tại", XpCost = 80, StockCount = 99, RewardType = "Privileged" });
            }

            // 3. Tải lịch sử đổi quà
            try
            {
                var redemptions = await _apiService.GetAsync<List<RedeemedRewardDto>>("/XpShop/my-redemptions");
                if (redemptions != null)
                {
                    MyRedemptions.Clear();
                    foreach (var red in redemptions)
                    {
                        MyRedemptions.Add(red);
                    }
                }
            }
            catch (Exception)
            {
                // Lịch sử đổi quà mock khi offline
                MyRedemptions.Clear();
                MyRedemptions.Add(new RedeemedRewardDto 
                { 
                    Id = 99, 
                    RewardName = "Mượn thêm 3 ngày", 
                    XpCost = 80, 
                    IsCollected = true, 
                    RewardType = "Privileged", 
                    RedeemedAt = DateTime.Now.AddDays(-2) 
                });
            }

            // 4. Tải lịch sử giao dịch XP
            try
            {
                var history = await _apiService.GetAsync<List<XpTransactionDto>>("/Users/xp-history");
                if (history != null)
                {
                    XpTransactions.Clear();
                    foreach (var tx in history)
                    {
                        XpTransactions.Add(tx);
                    }
                }
            }
            catch (Exception)
            {
                // Fallback offline mock transactions
                XpTransactions.Clear();
                XpTransactions.Add(new XpTransactionDto { Id = 1, Amount = 10, TransactionType = "Streak", Description = "Bắt đầu chuỗi đọc sách: Ngày 1 liên tiếp", CreatedAt = DateTime.Now.AddDays(-3) });
                XpTransactions.Add(new XpTransactionDto { Id = 2, Amount = 20, TransactionType = "Quiz", Description = "Hoàn thành Quiz đọc hiểu sách ID 1 (đạt 5/5 câu đúng)", CreatedAt = DateTime.Now.AddDays(-2) });
                XpTransactions.Add(new XpTransactionDto { Id = 3, Amount = -150, TransactionType = "Redeem", Description = "Đổi quà: Sổ tay xanh lá tái chế", CreatedAt = DateTime.Now.AddDays(-1) });
            }

            // Bổ sung các vật phẩm ảo trang trí vào danh mục của cửa hàng
            Rewards.Add(new XpRewardDto { Id = 101, Name = "Khung Neon Phát Sáng", Description = "Khung viền màu xanh neon phát sáng rực rỡ quanh ảnh đại diện", XpCost = 50, StockCount = 99, RewardType = "Border", ItemValue = "Neon" });
            Rewards.Add(new XpRewardDto { Id = 102, Name = "Khung Hoàng Gia Lấp Lánh", Description = "Khung viền mạ vàng quý phái tôn vinh học sinh tích cực", XpCost = 120, StockCount = 99, RewardType = "Border", ItemValue = "Gold" });
            Rewards.Add(new XpRewardDto { Id = 103, Name = "Khung Huyền Thoại Thần Thoại", Description = "Khung viền tím huyền thoại phát sáng cực mạnh", XpCost = 250, StockCount = 99, RewardType = "Border", ItemValue = "Mythic" });

            Rewards.Add(new XpRewardDto { Id = 201, Name = "Avatar Cáo Con Thông Thái", Description = "Biểu tượng đại diện hình chú Cáo dễ thương (🦊)", XpCost = 30, StockCount = 99, RewardType = "Avatar", ItemValue = "🦊" });
            Rewards.Add(new XpRewardDto { Id = 202, Name = "Avatar Pháp Sư Đọc Sách", Description = "Biểu tượng đại diện hình Pháp sư thông thái (🧙)", XpCost = 60, StockCount = 99, RewardType = "Avatar", ItemValue = "🧙" });
            Rewards.Add(new XpRewardDto { Id = 203, Name = "Avatar Robot Trợ Lý", Description = "Biểu tượng đại diện hình Robot thông minh (🤖)", XpCost = 80, StockCount = 99, RewardType = "Avatar", ItemValue = "🤖" });

            // Tải dữ liệu túi đồ cá nhân của học sinh
            ssoId = AuthService.CurrentUserSsoId ?? "HS001";
            progress = GamificationService.GetProgress(ssoId);
            EquippedAvatar = progress.EquippedAvatar ?? "👦";
            EquippedBorder = progress.EquippedBorder ?? "None";

            OwnedAvatars.Clear();
            foreach (var av in progress.OwnedAvatars ?? new List<string> { "👦", "👧" })
            {
                OwnedAvatars.Add(av);
            }

            OwnedBorders.Clear();
            foreach (var bd in progress.OwnedBorders ?? new List<string> { "None" })
            {
                OwnedBorders.Add(bd);
            }

            IsLoading = false;
            StatusMessage = "";
        }

        private async Task RedeemRewardAsync(XpRewardDto? reward)
        {
            if (reward == null) return;

            var ssoId = AuthService.CurrentUserSsoId ?? "HS001";
            var progress = GamificationService.GetProgress(ssoId);

            // Xử lý mua vật phẩm ảo
            if (reward.RewardType == "Border" || reward.RewardType == "Avatar")
            {
                bool alreadyOwned = reward.RewardType == "Border" 
                    ? progress.OwnedBorders.Contains(reward.ItemValue) 
                    : progress.OwnedAvatars.Contains(reward.ItemValue);

                if (alreadyOwned)
                {
                    MessageBox.Show("❌ Bạn đã sở hữu vật phẩm trang trí này rồi!", "Đã sở hữu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (progress.XP < reward.XpCost)
                {
                    MessageBox.Show($"❌ Bạn không đủ điểm XP để mua vật phẩm này!\nCần: {reward.XpCost} XP - Hiện có: {progress.XP} XP", "Không đủ XP", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var confirm = MessageBox.Show($"Bạn có chắc chắn muốn dùng {reward.XpCost} XP để mua trang trí '{reward.Name}'?", "Xác nhận mua", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

                IsLoading = true;
                
                // Trừ điểm và thêm vào túi đồ
                progress.XP -= reward.XpCost;
                if (reward.RewardType == "Border")
                    progress.OwnedBorders.Add(reward.ItemValue);
                else
                    progress.OwnedAvatars.Add(reward.ItemValue);

                // Lưu dữ liệu thông qua AddXp
                GamificationService.AddXp(ssoId, 0);

                UpdateMainViewModelXp();

                MessageBox.Show($"🎉 Mua trang trí thành công!\nHãy chuyển sang tab 'Túi đồ / Avatar' để trang bị vật phẩm mới.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                IsLoading = false;
                await LoadDataAsync();
                return;
            }

            if (progress.XP < reward.XpCost)
            {
                MessageBox.Show($"❌ Bạn không đủ điểm XP để đổi quà này!\nCần: {reward.XpCost} XP - Hiện có: {progress.XP} XP", "Không đủ XP", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (reward.StockCount <= 0)
            {
                MessageBox.Show("❌ Quà tặng này đã hết hàng trong kho!", "Hết hàng", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirmResult = MessageBox.Show($"Bạn có chắc chắn muốn dùng {reward.XpCost} XP để đổi lấy quà tặng '{reward.Name}'?", "Xác nhận đổi quà", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmResult != MessageBoxResult.Yes) return;

            IsLoading = true;
            try
            {
                // Gọi API đổi quà
                var response = await _apiService.PostAsync<RedeemRequestDto, RedeemResponseDto>("/XpShop/redeem", new RedeemRequestDto { RewardId = reward.Id });
                
                // Trừ điểm XP locally để đồng bộ
                GamificationService.AddXp(ssoId, -reward.XpCost);

                // Cập nhật MainViewModel
                UpdateMainViewModelXp();

                MessageBox.Show($"🎉 Đổi quà thành công!\n{response?.Message ?? "Hãy ra quầy thủ thư để nhận quà tặng của bạn."}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception)
            {
                // Giả lập offline
                GamificationService.AddXp(ssoId, -reward.XpCost);
                UpdateMainViewModelXp();

                MessageBox.Show($"🎉 Đổi quà thành công (Chế độ offline)!\nHãy mang mã đổi quà (Giao dịch ID: Offline-{new Random().Next(1000, 9999)}) ra quầy thủ thư khi kết nối được khôi phục.", "Thành công (Offline)", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            await LoadDataAsync();
        }

        private async Task EquipAvatarAsync(string? avatar)
        {
            if (string.IsNullOrEmpty(avatar)) return;
            var ssoId = AuthService.CurrentUserSsoId ?? "HS001";
            var progress = GamificationService.GetProgress(ssoId);
            
            progress.EquippedAvatar = avatar;
            GamificationService.AddXp(ssoId, 0); // Save data
            
            EquippedAvatar = avatar;
            UpdateMainViewModelXp();
            
            if (Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
            {
                mainVm.UpdateEquippedAvatarInfo();
            }
            
            MessageBox.Show($"👦 Đã đổi ảnh đại diện sang [{avatar}] thành công!", "Trang bị", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadDataAsync();
        }

        private async Task EquipBorderAsync(string? border)
        {
            if (string.IsNullOrEmpty(border)) return;
            var ssoId = AuthService.CurrentUserSsoId ?? "HS001";
            var progress = GamificationService.GetProgress(ssoId);
            
            progress.EquippedBorder = border;
            GamificationService.AddXp(ssoId, 0); // Save data
            
            EquippedBorder = border;
            UpdateMainViewModelXp();
            
            if (Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
            {
                mainVm.UpdateEquippedAvatarInfo();
            }
            
            MessageBox.Show($"🎨 Đã trang bị khung viền [{border}] thành công!", "Trang bị", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadDataAsync();
        }

        private void UpdateMainViewModelXp()
        {
            if (Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
            {
                var ssoId = AuthService.CurrentUserSsoId ?? "HS001";
                var progress = GamificationService.GetProgress(ssoId);
                mainVm.CurrentXP = progress.XP;
                mainVm.CurrentLevel = progress.Level;
                mainVm.MaxXP = GamificationService.GetMaxXpForLevel(progress.XP);
            }
        }
    }

    public class XpRewardDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int XpCost { get; set; }
        public int StockCount { get; set; }
        public string RewardType { get; set; } = "Physical";
        public string ItemValue { get; set; } = string.Empty;

        public string TypeIcon => RewardType switch
        {
            "Privileged" => "⚡",
            "Border" => "🎨",
            "Avatar" => "👤",
            _ => "🎁"
        };
        public string TypeName => RewardType switch
        {
            "Privileged" => "Đặc quyền",
            "Border" => "Trang trí Khung",
            "Avatar" => "Hình đại diện",
            _ => "Quà vật phẩm"
        };
        public string StockText => (RewardType == "Privileged" || RewardType == "Border" || RewardType == "Avatar") ? "Không giới hạn" : $"Còn lại: {StockCount} quà";
        public bool IsAvailable => StockCount > 0;
    }

    public class RedeemedRewardDto
    {
        public int Id { get; set; }
        public string RewardName { get; set; } = string.Empty;
        public int XpCost { get; set; }
        public bool IsCollected { get; set; }
        public string RewardType { get; set; } = "Physical";
        public DateTime RedeemedAt { get; set; }

        public string DisplayStatus => IsCollected ? "Đã nhận quà" : "Chờ nhận tại quầy ⌛";
        public string StatusColor => IsCollected ? "#10B981" : "#F59E0B";
        public string FormattedDate => RedeemedAt.ToString("dd/MM/yyyy HH:mm");
    }

    public class RedeemRequestDto
    {
        public int RewardId { get; set; }
    }

    public class RedeemResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public int RedemptionId { get; set; }
        public int XpCost { get; set; }
        public string RewardName { get; set; } = string.Empty;
        public int StockRemaining { get; set; }
    }

    public class XpTransactionDto
    {
        public int Id { get; set; }
        public int Amount { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string FormattedDate => CreatedAt.ToString("dd/MM/yyyy HH:mm");
        public string AmountText => Amount >= 0 ? $"+{Amount} XP" : $"{Amount} XP";
        public string AmountColor => Amount >= 0 ? "#10B981" : "#EF4444";
        public string TypeName => TransactionType switch
        {
            "Quiz" => "Làm Quiz đọc hiểu 📝",
            "Streak" => "Đọc sách hàng ngày 🔥",
            "Redeem" => "Đổi quà cửa hàng 🎁",
            "System" => "Hệ thống điều chỉnh ⚙️",
            _ => TransactionType
        };
    }
}
