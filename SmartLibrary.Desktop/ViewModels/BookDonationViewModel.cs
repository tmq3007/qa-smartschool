using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Services;
using SmartLibrary.Desktop.Helpers;
using System.Linq;

namespace SmartLibrary.Desktop.ViewModels;

public partial class BookDonationViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private string _bookTitle = string.Empty;

    [ObservableProperty]
    private string _author = string.Empty;

    [ObservableProperty]
    private string _selectedCondition = "Tốt (Good)";

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _successMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading = false;

    public string[] Conditions { get; } = new[] { "Rất tốt (Excellent)", "Tốt (Good)", "Cũ (Fair)" };

    public ObservableCollection<DonationItemViewModel> MyDonations { get; } = new();
    public ObservableCollection<DonorLeaderboardItem> Leaderboard { get; } = new();

    public ICommand DonateCommand { get; }
    public ICommand RefreshCommand { get; }

    public BookDonationViewModel(ApiService apiService)
    {
        _apiService = apiService;
        DonateCommand = new AsyncRelayCommand(DonateBookAsync);
        RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
    }

    public async Task LoadDataAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            await Task.WhenAll(LoadMyDonationsAsync(), LoadLeaderboardAsync());
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi tải dữ liệu: {ex.Message}";
            LoadFallbackMockData();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadMyDonationsAsync()
    {
        try
        {
            var result = await _apiService.GetAsync<DonationItemDto[]>("/Donations/my-donations");
            MyDonations.Clear();
            if (result != null)
            {
                foreach (var item in result)
                {
                    MyDonations.Add(new DonationItemViewModel
                    {
                        Id = item.Id,
                        BookTitle = item.BookTitle,
                        Author = item.Author,
                        BookCondition = item.BookCondition,
                        IsProcessed = item.IsProcessed,
                        DonatedAt = item.DonatedAt
                    });
                }
            }
        }
        catch
        {
            // Load local mock history if offline
            LoadMockMyDonations();
        }
    }

    private async Task LoadLeaderboardAsync()
    {
        // Mock Leaderboard bảng vàng vinh danh
        await Task.Delay(100); // Giả lập network delay ngắn
        Leaderboard.Clear();
        Leaderboard.Add(new DonorLeaderboardItem { Rank = 1, FullName = "Nguyễn Văn An", ClassName = "11A1", TotalDonated = 8, Medal = "🥇" });
        Leaderboard.Add(new DonorLeaderboardItem { Rank = 2, FullName = "Trần Thị Bình", ClassName = "11A1", TotalDonated = 5, Medal = "🥈" });
        Leaderboard.Add(new DonorLeaderboardItem { Rank = 3, FullName = "Lê Hoàng Cường", ClassName = "11A2", TotalDonated = 4, Medal = "🥉" });
        Leaderboard.Add(new DonorLeaderboardItem { Rank = 4, FullName = "Võ Minh Em", ClassName = "12A3", TotalDonated = 3, Medal = "⭐" });
        Leaderboard.Add(new DonorLeaderboardItem { Rank = 5, FullName = "Phan Văn Khánh", ClassName = "10A1", TotalDonated = 2, Medal = "⭐" });
    }

    private void LoadFallbackMockData()
    {
        LoadMockMyDonations();
        _ = LoadLeaderboardAsync();
    }

    private void LoadMockMyDonations()
    {
        MyDonations.Clear();
        MyDonations.Add(new DonationItemViewModel
        {
            Id = 1,
            BookTitle = "Số Đỏ",
            Author = "Vũ Trọng Phụng",
            BookCondition = "Tốt (Good)",
            IsProcessed = true,
            DonatedAt = DateTime.UtcNow.AddDays(-5)
        });
        MyDonations.Add(new DonationItemViewModel
        {
            Id = 2,
            BookTitle = "Nhà Giả Kim",
            Author = "Paulo Coelho",
            BookCondition = "Rất tốt (Excellent)",
            IsProcessed = false,
            DonatedAt = DateTime.UtcNow.AddDays(-1)
        });
    }

    private async Task DonateBookAsync()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        // Tránh lập trình viên làm sai: chuẩn hóa dữ liệu đầu vào thông qua InputHelper
        var normalizedTitle = InputHelper.NormalizeInput(BookTitle);
        var normalizedAuthor = InputHelper.NormalizeInput(Author);

        if (!InputHelper.ValidateLength(normalizedTitle, 2, 150))
        {
            ErrorMessage = "Tên sách phải từ 2 đến 150 ký tự!";
            return;
        }

        if (!InputHelper.ValidateLength(normalizedAuthor, 2, 100))
        {
            ErrorMessage = "Tên tác giả phải từ 2 đến 100 ký tự!";
            return;
        }

        IsLoading = true;

        try
        {
            var payload = new
            {
                BookTitle = normalizedTitle,
                Author = normalizedAuthor,
                BookCondition = SelectedCondition
            };

            await _apiService.PostAsync<object, object>("/Donations", payload);

            SuccessMessage = $"🎉 Cảm ơn bạn! Đã gửi đề xuất quyên góp sách '{normalizedTitle}' thành công. Đang chờ phê duyệt.";
            
            // Xóa sạch form
            BookTitle = string.Empty;
            Author = string.Empty;
            SelectedCondition = "Tốt (Good)";

            // Cập nhật điểm thưởng XP lập tức cho học sinh ở Client
            var ssoId = AuthService.CurrentUserSsoId ?? "HS001";
            var result = GamificationService.AddXp(ssoId, 100);
            
            // Đồng bộ trực tiếp điểm XP hiển thị lên Main Window nếu có
            if (System.Windows.Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
            {
                mainVm.CurrentXP = result.NewXp;
                mainVm.CurrentLevel = result.NewLevel;
                mainVm.MaxXP = GamificationService.GetMaxXpForLevel(result.NewXp);

                if (result.LeveledUp)
                {
                    System.Windows.MessageBox.Show(
                        $"✨ CHÚC MỪNG! Bạn đã thăng cấp từ [{result.OldLevel}] lên [{result.NewLevel}]! ✨",
                        "Thăng Cấp", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }

            // Load lại danh sách cá nhân
            await LoadMyDonationsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể gửi quyên góp: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public class DonationItemDto
{
    public int Id { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string BookCondition { get; set; } = string.Empty;
    public bool IsProcessed { get; set; }
    public DateTime DonatedAt { get; set; }
}

public class DonationItemViewModel
{
    public int Id { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string BookCondition { get; set; } = string.Empty;
    public bool IsProcessed { get; set; }
    public DateTime DonatedAt { get; set; }

    public string StatusText => IsProcessed ? "Đã Phê Duyệt" : "Chờ Duyệt";
    public string StatusColor => IsProcessed ? "#10B981" : "#F59E0B"; // Emerald vs Amber
    public string FormattedDate => DonatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
}

public class DonorLeaderboardItem
{
    public int Rank { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public int TotalDonated { get; set; }
    public string Medal { get; set; } = string.Empty;
}
