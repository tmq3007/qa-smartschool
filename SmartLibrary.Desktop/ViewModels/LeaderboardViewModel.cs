using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using SmartLibrary.Desktop.Services;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class LeaderboardViewModel : ObservableObject
    {
        private readonly ApiService? _apiService;

        [ObservableProperty] private string _myName = "";
        [ObservableProperty] private string _myLevel = "";
        [ObservableProperty] private int _myXP;
        [ObservableProperty] private int _myMaxXP = 500;
        [ObservableProperty] private string _myRank = "N/A";

        [ObservableProperty] private string _myEquippedAvatar = "👦";
        [ObservableProperty] private System.Windows.Media.Brush _myEquippedBorderBrush = System.Windows.Media.Brushes.Transparent;
        [ObservableProperty] private System.Windows.Thickness _myEquippedBorderThickness = new System.Windows.Thickness(0);
        [ObservableProperty] private System.Windows.Media.Effects.Effect? _myEquippedBorderEffect = null;

        public ObservableCollection<StudentLeaderboardItem> TopStudents { get; } = new();
        public ObservableCollection<ClassLeaderboardItem> TopClasses { get; } = new();
        public ObservableCollection<BadgeItem> Badges { get; } = new();

        public LeaderboardViewModel(ApiService? apiService = null)
        {
            _apiService = apiService;
            if (_apiService != null)
            {
                _ = LoadDataFromServerAsync();
            }
            else
            {
                LoadData();
            }
        }

        public async Task LoadDataFromServerAsync()
        {
            try
            {
                var data = await _apiService!.GetAsync<LeaderboardResponseDto>("/Reports/leaderboard");
                if (data != null)
                {
                    TopStudents.Clear();
                    int sRank = 1;
                    foreach (var s in data.TopStudents)
                    {
                        string trophy = sRank == 1 ? "🥇" : (sRank == 2 ? "🥈" : (sRank == 3 ? "🥉" : "⭐"));
                        TopStudents.Add(new StudentLeaderboardItem
                        {
                            Rank = sRank++,
                            StudentName = s.StudentName,
                            ClassName = s.ClassName,
                            BooksCount = s.BooksCount,
                            Trophy = trophy
                        });
                    }

                    TopClasses.Clear();
                    int cRank = 1;
                    foreach (var c in data.TopClasses)
                    {
                        string trophy = cRank == 1 ? "🏆" : (cRank == 2 ? "🥈" : (cRank == 3 ? "🥉" : "⭐"));
                        TopClasses.Add(new ClassLeaderboardItem
                        {
                            Rank = cRank++,
                            ClassName = c.ClassName,
                            BooksCount = c.BooksCount,
                            Trophy = trophy
                        });
                    }
                    UpdateMyStats();
                }
                else
                {
                    LoadData();
                }
            }
            catch
            {
                LoadData(); // Fallback to mock if offline
            }
        }

        private void LoadData()
        {
            TopStudents.Clear();
            TopStudents.Add(new StudentLeaderboardItem { Rank = 1, StudentName = "Nguyễn Văn An", ClassName = "11A1", BooksCount = 15, Trophy = "🥇" });
            TopStudents.Add(new StudentLeaderboardItem { Rank = 2, StudentName = "Trần Thị Bình", ClassName = "11A1", BooksCount = 12, Trophy = "🥈" });
            TopStudents.Add(new StudentLeaderboardItem { Rank = 3, StudentName = "Lê Hoàng Cường", ClassName = "11A2", BooksCount = 10, Trophy = "🥉" });
            TopStudents.Add(new StudentLeaderboardItem { Rank = 4, StudentName = "Phạm Minh Đức", ClassName = "10C1", BooksCount = 9, Trophy = "⭐" });
            TopStudents.Add(new StudentLeaderboardItem { Rank = 5, StudentName = "Võ Thị Em", ClassName = "12A3", BooksCount = 8, Trophy = "⭐" });
            TopStudents.Add(new StudentLeaderboardItem { Rank = 6, StudentName = "Hoàng Văn Gia", ClassName = "10B2", BooksCount = 7, Trophy = "⭐" });
            TopStudents.Add(new StudentLeaderboardItem { Rank = 7, StudentName = "Nguyễn Thị Hoa", ClassName = "11A3", BooksCount = 6, Trophy = "⭐" });
            TopStudents.Add(new StudentLeaderboardItem { Rank = 8, StudentName = "Bùi Văn Hùng", ClassName = "12A1", BooksCount = 5, Trophy = "⭐" });
            TopStudents.Add(new StudentLeaderboardItem { Rank = 9, StudentName = "Đỗ Thị Quỳnh", ClassName = "11A2", BooksCount = 5, Trophy = "⭐" });
            TopStudents.Add(new StudentLeaderboardItem { Rank = 10, StudentName = "Phan Văn Khánh", ClassName = "10A1", BooksCount = 4, Trophy = "⭐" });

            TopClasses.Clear();
            TopClasses.Add(new ClassLeaderboardItem { Rank = 1, ClassName = "11A1", BooksCount = 125, Trophy = "🏆" });
            TopClasses.Add(new ClassLeaderboardItem { Rank = 2, ClassName = "11A2", BooksCount = 98, Trophy = "🥈" });
            TopClasses.Add(new ClassLeaderboardItem { Rank = 3, ClassName = "12A1", BooksCount = 85, Trophy = "🥉" });
            TopClasses.Add(new ClassLeaderboardItem { Rank = 4, ClassName = "10C1", BooksCount = 76, Trophy = "⭐" });
            TopClasses.Add(new ClassLeaderboardItem { Rank = 5, ClassName = "12A3", BooksCount = 68, Trophy = "⭐" });
            UpdateMyStats();
        }

        private void UpdateMyStats()
        {
            var ssoId = AuthService.CurrentUserSsoId ?? "HS001";
            MyName = AuthService.CurrentUserName ?? "Độc giả học sinh";
            
            // Tìm và hiển thị tiến trình XP
            int xpVal = 120;
            string lvlVal = "Cấp 1: Độc giả Đồng";
            try
            {
                var progress = GamificationService.GetProgress(ssoId);
                MyXP = progress.XP;
                MyLevel = progress.Level;
                MyMaxXP = GamificationService.GetMaxXpForLevel(progress.XP);
                xpVal = progress.XP;
                lvlVal = progress.Level;
            }
            catch
            {
                MyLevel = "Cấp 1: Độc giả Đồng";
                MyXP = 120;
                MyMaxXP = 500;
            }

            var myRankItem = TopStudents.FirstOrDefault(s => s.StudentName == MyName);
            MyRank = myRankItem != null ? myRankItem.Rank.ToString() : "Ngoài Top 10";

            UpdateMyAvatarInfo();
            _ = UpdateBadgesAsync(ssoId, xpVal, lvlVal);
        }

        public void UpdateMyAvatarInfo()
        {
            var ssoId = AuthService.CurrentUserSsoId ?? "HS001";
            var progress = GamificationService.GetProgress(ssoId);
            MyEquippedAvatar = progress.EquippedAvatar ?? "👦";
            
            string border = progress.EquippedBorder ?? "None";
            switch (border)
            {
                case "Neon":
                    MyEquippedBorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00F5FF"));
                    MyEquippedBorderThickness = new System.Windows.Thickness(2.5);
                    MyEquippedBorderEffect = new System.Windows.Media.Effects.DropShadowEffect 
                    { 
                        Color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00F5FF"), 
                        BlurRadius = 10, 
                        ShadowDepth = 0, 
                        Opacity = 0.85 
                    };
                    break;
                case "Gold":
                    MyEquippedBorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFD700"));
                    MyEquippedBorderThickness = new System.Windows.Thickness(2.5);
                    MyEquippedBorderEffect = new System.Windows.Media.Effects.DropShadowEffect 
                    { 
                        Color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFD700"), 
                        BlurRadius = 8, 
                        ShadowDepth = 0, 
                        Opacity = 0.8 
                    };
                    break;
                case "Mythic":
                    MyEquippedBorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#A855F7"));
                    MyEquippedBorderThickness = new System.Windows.Thickness(3);
                    MyEquippedBorderEffect = new System.Windows.Media.Effects.DropShadowEffect 
                    { 
                        Color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#A855F7"), 
                        BlurRadius = 12, 
                        ShadowDepth = 0, 
                        Opacity = 0.9 
                    };
                    break;
                default:
                    MyEquippedBorderBrush = System.Windows.Media.Brushes.Transparent;
                    MyEquippedBorderThickness = new System.Windows.Thickness(0);
                    MyEquippedBorderEffect = null;
                    break;
            }
        }

        private async Task UpdateBadgesAsync(string ssoId, int xp, string level)
        {
            int approvedCount = 0;
            if (_apiService != null)
            {
                try
                {
                    var contributions = await _apiService.GetAsync<System.Collections.Generic.List<MyContributionDto>>("/Audiobook/my-contributions", silent: true);
                    if (contributions != null)
                    {
                        approvedCount = contributions.Count(c => c.ApprovedStatus == 1);
                    }
                }
                catch
                {
                    approvedCount = 0;
                }
            }
            else
            {
                // Fallback offline / mock
                if (ssoId == "HS001")
                {
                    approvedCount = 5; // Unlock for default mock student
                }
            }

            int levelNumber = 1;
            if (!string.IsNullOrEmpty(level))
            {
                var parts = level.Split(':');
                if (parts.Length > 0 && parts[0].Contains("Cấp"))
                {
                    var numStr = parts[0].Replace("Cấp", "").Trim();
                    if (int.TryParse(numStr, out int lNum))
                    {
                        levelNumber = lNum;
                    }
                }
            }

            var goldVoice = new BadgeItem
            {
                Name = "Giọng đọc vàng",
                Description = $"Đóng góp 5 sách nói được phê duyệt. (Đạt: {approvedCount}/5)",
                Icon = "🎙️",
                IsUnlocked = approvedCount >= 5,
                StatusText = approvedCount >= 5 ? "Đã mở khóa" : "Chưa đạt",
                StatusColor = approvedCount >= 5 ? "#10B981" : "#94A3B8",
                UnlockCondition = "Thu âm đóng góp tối thiểu 5 tệp sách nói được duyệt thành công"
            };

            var youngReader = new BadgeItem
            {
                Name = "Mầm non Đọc sách",
                Description = "Đã tích lũy điểm XP từ hoạt động đọc sách đầu tiên.",
                Icon = "🌱",
                IsUnlocked = xp > 0,
                StatusText = xp > 0 ? "Đã mở khóa" : "Chưa đạt",
                StatusColor = xp > 0 ? "#10B981" : "#94A3B8",
                UnlockCondition = "Đọc cuốn sách đầu tiên trên thư viện số"
            };

            var masterReader = new BadgeItem
            {
                Name = "Độc giả Siêu cấp",
                Description = $"Đạt từ Cấp 5 trở lên trong học tập. (Hiện tại: Cấp {levelNumber})",
                Icon = "⚡",
                IsUnlocked = levelNumber >= 5,
                StatusText = levelNumber >= 5 ? "Đã mở khóa" : "Chưa đạt",
                StatusColor = levelNumber >= 5 ? "#10B981" : "#94A3B8",
                UnlockCondition = "Đạt từ Cấp 5 trở lên trong học tập từ việc tích lũy điểm XP qua các hoạt động của thư viện số"
            };

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Badges.Clear();
                Badges.Add(goldVoice);
                Badges.Add(youngReader);
                Badges.Add(masterReader);
            });
        }
    }

    public partial class BadgeItem : ObservableObject
    {
        [ObservableProperty] private string _name = "";
        [ObservableProperty] private string _description = "";
        [ObservableProperty] private string _icon = "";
        [ObservableProperty] private bool _isUnlocked;
        [ObservableProperty] private string _statusText = "";
        [ObservableProperty] private string _statusColor = "";
        [ObservableProperty] private string _unlockCondition = "";
    }

    public class LeaderboardResponseDto
    {
        public System.Collections.Generic.List<StudentLeaderboardResponseDto> TopStudents { get; set; } = new();
        public System.Collections.Generic.List<ClassLeaderboardResponseDto> TopClasses { get; set; } = new();
    }

    public class StudentLeaderboardResponseDto
    {
        public string StudentName { get; set; } = "";
        public string ClassName { get; set; } = "";
        public int BooksCount { get; set; }
    }

    public class ClassLeaderboardResponseDto
    {
        public string ClassName { get; set; } = "";
        public int BooksCount { get; set; }
    }

    public class StudentLeaderboardItem
    {
        public int Rank { get; set; }
        public string StudentName { get; set; } = "";
        public string ClassName { get; set; } = "";
        public int BooksCount { get; set; }
        public string Trophy { get; set; } = "";
    }

    public class ClassLeaderboardItem
    {
        public int Rank { get; set; }
        public string ClassName { get; set; } = "";
        public int BooksCount { get; set; }
        public string Trophy { get; set; } = "";
    }
}
