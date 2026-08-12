using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace QASmartClass.TeacherHub.Views
{
    public partial class ClassMIDashboardView : UserControl
    {
        private AppDbContext? _db;

        public ClassMIDashboardView()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _db = new AppDbContext();
            LoadRosters();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _db?.Dispose();
            _db = null;
        }

        private void LoadRosters()
        {
            if (_db == null) return;
            var rosters = _db.ClassRosters.Select(r => r.ClassName).Distinct().ToList();
            if (rosters.Count == 0) rosters.Add("10A1"); // Fallback
            CbRoster.ItemsSource = rosters;
            if (rosters.Any()) CbRoster.SelectedIndex = 0;
        }

        private async void CbRoster_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_db == null) return;
            string className = CbRoster.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrEmpty(className)) return;

            TxtLoading.Visibility = Visibility.Visible;

            try
            {
                // TC-MI-005: Use AsNoTracking for fast rendering of 50+ students
                var students = await _db.Students
                    .AsNoTracking()
                    .Where(s => s.ClassName == className)
                    .Select(s => new { s.Id, s.FullName })
                    .ToListAsync();

                var studentIds = students.Select(s => s.Id).ToList();

                var profiles = await _db.StudentMIProfiles
                    .AsNoTracking()
                    .Where(p => studentIds.Contains(p.StudentId))
                    .ToListAsync();

                if (profiles.Count == 0)
                {
                    ClassRadarCanvas.Children.Clear();
                    DgTopStudents.ItemsSource = null;
                    DgAtRiskStudents.ItemsSource = null;
                    return;
                }

                // 1. Calculate Averages
                double avgLogical = profiles.Average(p => p.LogicalScore);
                double avgSpatial = profiles.Average(p => p.SpatialScore);
                double avgMusical = profiles.Average(p => p.MusicalScore);
                double avgKines = profiles.Average(p => p.KinestheticScore);
                double avgInter = profiles.Average(p => p.InterpersonalScore);
                double avgIntra = profiles.Average(p => p.IntrapersonalScore);
                double avgNature = profiles.Average(p => p.NaturalisticScore);
                double avgLing = profiles.Average(p => p.LinguisticScore);

                DrawClassRadar(new[] { avgLogical, avgSpatial, avgMusical, avgKines, avgInter, avgIntra, avgNature, avgLing });

                // 2. Find Top Students (Percent)
                var topList = new List<dynamic>
                {
                    GetTop("Toán/Logic", profiles, students, p => p.LogicalScore, avgLogical),
                    GetTop("Không gian", profiles, students, p => p.SpatialScore, avgSpatial),
                    GetTop("Âm nhạc", profiles, students, p => p.MusicalScore, avgMusical),
                    GetTop("Vận động", profiles, students, p => p.KinestheticScore, avgKines),
                    GetTop("Giao tiếp", profiles, students, p => p.InterpersonalScore, avgInter),
                    GetTop("Nội tâm", profiles, students, p => p.IntrapersonalScore, avgIntra),
                    GetTop("Tự nhiên", profiles, students, p => p.NaturalisticScore, avgNature),
                    GetTop("Ngôn ngữ", profiles, students, p => p.LinguisticScore, avgLing)
                };

                DgTopStudents.ItemsSource = topList;

                // 3. TASK 3.1: At-Risk Students (Vùng r?i ro)
                double classAvgTotalXp = (avgLogical + avgSpatial + avgMusical + avgKines + avgInter + avgIntra + avgNature + avgLing);
                double threshold = classAvgTotalXp * 0.25;

                var atRiskList = new List<dynamic>();
                foreach (var st in students)
                {
                    var p = profiles.FirstOrDefault(x => x.StudentId == st.Id);
                    double totalXp = 0;
                    if (p != null)
                    {
                        totalXp = p.LogicalScore + p.SpatialScore + p.MusicalScore + p.KinestheticScore + 
                                  p.InterpersonalScore + p.IntrapersonalScore + p.NaturalisticScore + p.LinguisticScore;
                    }
                    
                    if (totalXp < threshold)
                    {
                        atRiskList.Add(new {
                            Id = st.Id,
                            FullName = st.FullName,
                            TotalXP = totalXp,
                            Warning = totalXp == 0 ? "Chưa có dữ liệu hoạt động" : "Cần hỗ trợ tương tác"
                        });
                    }
                }
                
                DgAtRiskStudents.ItemsSource = atRiskList.OrderBy(x => x.TotalXP).ToList();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "ClassMIDashboard Load error");
            }
            finally
            {
                TxtLoading.Visibility = Visibility.Collapsed;
            }
        }

        private dynamic GetTop(string type, List<StudentMIProfile> profiles, IEnumerable<dynamic> students, Func<StudentMIProfile, double> selector, double avg)
        {
            var top = profiles.OrderByDescending(selector).FirstOrDefault();
            string name = "N/A";
            double score = 0;

            if (top != null)
            {
                score = Math.Round(selector(top), 1);
                var st = Enumerable.FirstOrDefault(students, s => s.Id == top.StudentId);
                if (st != null) name = st.FullName;
            }

            return new
            {
                MIType = type,
                AverageScore = Math.Round(avg, 1),
                TopStudentName = name,
                TopStudentScore = score
            };
        }

        private void DrawClassRadar(double[] scores)
        {
            ClassRadarCanvas.Children.Clear();
            
            double centerX = 130, centerY = 130;
            double maxRadius = 90;

            // Background Grid (5 levels)
            for (int r = 18; r <= 90; r += 18)
            {
                var bgPoly = new Polygon { Stroke = new SolidColorBrush(Color.FromRgb(226, 232, 240)), StrokeThickness = 1 };
                for (int i = 0; i < 8; i++)
                {
                    double angle = i * (Math.PI / 4) - Math.PI / 2;
                    bgPoly.Points.Add(new Point(centerX + r * Math.Cos(angle), centerY + r * Math.Sin(angle)));
                }
                ClassRadarCanvas.Children.Add(bgPoly);
            }

            // Draw Data Polygon - Teacher Mode
            var dataPoly = new Polygon 
            { 
                Fill = new SolidColorBrush(Color.FromArgb(90, 16, 185, 129)), // Emerald color for Teacher
                Stroke = new SolidColorBrush(Color.FromRgb(5, 150, 105)), 
                StrokeThickness = 2 
            };
            
            string[] labels = { "Toán/Logic", "Không gian", "Âm nhạc", "Vận động", "Giao tiếp", "Nội tâm", "Tự nhiên", "Ngôn ngữ" };
            double totalRaw = scores.Sum();

            const double Max_XP = 100.0;
            for (int i = 0; i < 8; i++)
            {
                double angle = i * (Math.PI / 4) - Math.PI / 2;
                double r = (scores[i] / Max_XP) * maxRadius;
                
                r = Math.Max(5, r);
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
                
                double labelRadius = maxRadius + 20;
                Canvas.SetLeft(lbl, centerX + labelRadius * Math.Cos(angle) - 25);
                Canvas.SetTop(lbl, centerY + labelRadius * Math.Sin(angle) - 8);
                ClassRadarCanvas.Children.Add(lbl);
            }

            ClassRadarCanvas.Children.Add(dataPoly);
        }
    }
}

