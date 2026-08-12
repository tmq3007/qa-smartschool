using QASmartClass.Data;
using QASmartClass.Services;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartClass.StudentClient.Views
{
    public partial class LeaderboardView : Page
    {
        private readonly RewardService _reward;
        private readonly int _studentId;
        private readonly string _className;

        public LeaderboardView(AppDbContext db, int studentId, string className)
        {
            InitializeComponent();
            _reward = new RewardService(db);
            _studentId = studentId;
            _className = className;
            Loaded += (_, __) => LoadData(db);
        }

        private void LoadData(AppDbContext db)
        {
            try
            {
                // My XP
                var student = db.Students.Find(_studentId);
                TxtMyXp.Text = $"XP của bạn: {student?.TotalXp ?? 0}";

                // My Badges
                var badges = _reward.GetStudentBadges(_studentId);
                if (badges.Any())
                    LstBadges.ItemsSource = badges;
                else
                    TxtNoBadge.Visibility = Visibility.Visible;

                // Leaderboard
                var board = _reward.GetLeaderboard(_className, 10);
                var green  = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                var silver = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                var bronze = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));

                LstLeaderboard.ItemsSource = board.Select(b => new
                {
                    b.Rank,
                    b.StudentName,
                    RankDisplay = b.Rank <= 3 ? new[] { "\U0001f947", "\U0001f948", "\U0001f949" }[b.Rank - 1] : $"#{b.Rank}",
                    RankColor = b.Rank switch
                    {
                        1 => green,
                        2 => silver,
                        3 => bronze,
                        _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"))
                    },
                    XpDisplay = $"{b.TotalXp} XP",
                    BadgeDisplay = b.BadgeCount > 0 ? $"{b.BadgeCount} huy hiệu" : ""
                }).ToList();

                Log.Information("[Leaderboard] Loaded for class {Class}", _className);

                // Removed blocking MessageBox and N+1 db.Badges.Find
            }
            catch (Exception ex) { Log.Warning("[Leaderboard] Load error: {Err}", ex.Message); }
        }
    }
}

