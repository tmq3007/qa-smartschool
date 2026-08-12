using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.StudentClient.Views.MIGames;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Animation;

namespace QASmartClass.StudentClient.Views
{
    public partial class GameHubPage : Page
    {
        private readonly AppDbContext _db;
        private readonly int _studentId;
        private readonly MIAnalysisService _miService;
        private string _activeGameType = "";

        public GameHubPage(AppDbContext db, int studentId)
        {
            InitializeComponent();
            _db = db;
            _studentId = studentId;
            _miService = new MIAnalysisService(_db);
            LoadMiProfile();

            Unloaded += (s, e) =>
            {
                if (GameContainer.Content is IMiGameControl oldGame)
                {
                    oldGame.OnGameOver -= HandleGameControlOver;
                }
            };
        }

        private void LoadMiProfile()
        {
            var profile = _db.StudentMIProfiles.FirstOrDefault(p => p.StudentId == _studentId);
            if (profile == null)
            {
                profile = new StudentMIProfile { StudentId = _studentId };
                _db.StudentMIProfiles.Add(profile);
                _db.SaveChanges();
            }

            DrawRadarChart(profile);
            
            var lastRecord = _db.MiniGameRecords.Where(r => r.StudentId == _studentId).OrderByDescending(r => r.PlayedAt).FirstOrDefault();
            if (lastRecord != null && !string.IsNullOrEmpty(lastRecord.AiFeedback))
                TxtAiCoach.Text = lastRecord.AiFeedback;
            else
                TxtAiCoach.Text = "🤖 AI Coach: Chào mừng bạn đến với Hệ sinh thái MI! Chơi game để mình đánh giá nhé.";

            // Load Streak Count
            var student = _db.Students.FirstOrDefault(s => s.Id == _studentId);
            if (student != null)
            {
                TxtStreakCount.Text = $"{student.DailyStreak} ngày liên tiếp";
            }
            else
            {
                TxtStreakCount.Text = "0 ngày liên tiếp";
            }

            // Load Top 10 Leaderboard
            try
            {
                var topStudents = _db.Students
                                     .Where(s => s.Status == "Active")
                                     .OrderByDescending(s => s.TotalXp)
                                     .Take(10)
                                     .ToList();
                var leaderboard = topStudents.Select((s, index) => new {
                                         Rank = index + 1,
                                         FullName = s.FullName,
                                         ClassName = s.ClassName,
                                         TotalXp = s.TotalXp
                                     })
                                     .ToList();
                DgLeaderboard.ItemsSource = leaderboard;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to load leaderboard in GameHubPage");
            }
        }

        private void DrawRadarChart(StudentMIProfile p)
        {
            RadarCanvas.Children.Clear();
            
            double centerX = 140, centerY = 140;
            double maxRadius = 100;

            // Background Grid (5 levels)
            for (int r = 20; r <= 100; r += 20)
            {
                var bgPoly = new Polygon { Stroke = new SolidColorBrush(Color.FromRgb(226, 232, 240)), StrokeThickness = 1 };
                for (int i = 0; i < 8; i++)
                {
                    double angle = i * (Math.PI / 4) - Math.PI / 2;
                    bgPoly.Points.Add(new Point(centerX + r * Math.Cos(angle), centerY + r * Math.Sin(angle)));
                }
                RadarCanvas.Children.Add(bgPoly);
            }

            // TASK 2.2: Thuật toán tỷ trọng Radar Chart
            double[] rawScores = { p.LogicalScore, p.SpatialScore, p.MusicalScore, p.KinestheticScore, p.InterpersonalScore, p.IntrapersonalScore, p.NaturalisticScore, p.LinguisticScore };
            string[] labels = { "Toán/Logic", "Không gian", "Âm nhạc", "Vận động", "Giao tiếp", "Nội tâm", "Tự nhiên", "Ngôn ngữ" };
            double totalRaw = rawScores.Sum();
            
            var dataPoly = new Polygon 
            { 
                Fill = new SolidColorBrush(Color.FromArgb(90, 59, 130, 246)), 
                Stroke = new SolidColorBrush(Color.FromRgb(37, 99, 235)), 
                StrokeThickness = 2 
            };

            for (int i = 0; i < 8; i++)
            {
                double angle = i * (Math.PI / 4) - Math.PI / 2;
                
                // Normalization
                double percent = totalRaw == 0 ? 0.125 : (rawScores[i] / totalRaw);
                double r = percent * maxRadius * 3; // Scale up for visual (max is usually around 30-40% per trait)
                
                // Cap visual at maxRadius
                r = Math.Max(5, r); // Minimum 5px to avoid flat center
                r = Math.Min(maxRadius, r);
                
                dataPoly.Points.Add(new Point(centerX + r * Math.Cos(angle), centerY + r * Math.Sin(angle)));

                // Add Labels
                var lbl = new TextBlock 
                { 
                    Text = labels[i], 
                    FontSize = 11, 
                    Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                    FontWeight = FontWeights.SemiBold
                };
                
                double labelRadius = maxRadius + 25;
                Canvas.SetLeft(lbl, centerX + labelRadius * Math.Cos(angle) - 25);
                Canvas.SetTop(lbl, centerY + labelRadius * Math.Sin(angle) - 8);
                RadarCanvas.Children.Add(lbl);
            }

            RadarCanvas.Children.Add(dataPoly);
        }

        private void HandleGameControlOver(bool isCorrect, int xp)
        {
            HandleGameOver(_activeGameType, isCorrect, xp);
        }

        private void PlayGame_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string gameType = btn.Tag?.ToString() ?? "Logical";
                
                // Cleanup to avoid Memory Leak
                if (GameContainer.Content is IMiGameControl oldGame)
                {
                    oldGame.OnGameOver -= HandleGameControlOver;
                }
                
                GameContainer.Content = null;

                // Load New View Dynamically via Reflection
                try
                {
                    _activeGameType = gameType;
                    var typeName = $"QASmartClass.StudentClient.Views.MIGames.{gameType}GameView";
                    var assembly = typeof(GameHubPage).Assembly;
                    var type = assembly.GetType(typeName);
                    if (type == null)
                    {
                        // Fallback to Logical for unregistered/unimplemented game types
                        type = typeof(MIGames.LogicalGameView);
                        _activeGameType = "Logical";
                    }

                    if (type != null)
                    {
                        var gameInstance = Activator.CreateInstance(type) as IMiGameControl;
                        if (gameInstance != null)
                        {
                            gameInstance.OnGameOver += HandleGameControlOver;
                            GameContainer.Content = gameInstance;
                            gameInstance.StartGame();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "Failed to dynamically load game {GameType}", gameType);
                }
            }
        }

        private async void HandleGameOver(string targetMI, bool isCorrect, int xpGained)
        {
            if (isCorrect)
                System.Media.SystemSounds.Asterisk.Play();
            else
                System.Media.SystemSounds.Beep.Play();

            // Hide game
            GameContainer.Content = null;

            // Show float animation
            TxtFloatXp.Text = isCorrect ? $"+{xpGained} XP" : "0 XP (SAI)";
            TxtFloatXp.Foreground = isCorrect ? new SolidColorBrush(Color.FromRgb(16, 185, 129)) : new SolidColorBrush(Color.FromRgb(239, 68, 68));
            TxtFloatXp.Visibility = Visibility.Visible;
            
            DoubleAnimation floatAnim = new DoubleAnimation(-50, TimeSpan.FromSeconds(1));
            DoubleAnimation fadeAnim = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(1));
            
            FloatTransform.BeginAnimation(TranslateTransform.YProperty, floatAnim);
            TxtFloatXp.BeginAnimation(UIElement.OpacityProperty, fadeAnim);

            // DB Process
            string fb = await _miService.SubmitGameResultAsync(_studentId, $"Game {targetMI}", targetMI, xpGained, 30, isCorrect);
            
            // Reload UI
            LoadMiProfile();
            TxtAiCoach.Text = fb;
        }
    }
}
