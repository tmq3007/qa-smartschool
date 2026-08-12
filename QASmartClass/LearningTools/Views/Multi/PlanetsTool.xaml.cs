using System;
using System.Collections.Generic;
using System.Linq;
using QASmartClass.LearningTools.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using QASmartClass.LearningTools.Controls;
using Microsoft.Data.Sqlite;

namespace QASmartClass.LearningTools.Views.Multi
{
    public partial class PlanetsTool : BaseToolControl
    {
        private Grid? _selectedPlanetNode = null;
        private bool _isPaused = false;
        private Planet[] _loadedPlanets = Array.Empty<Planet>();
        private System.Windows.Threading.DispatcherTimer? _tutorialTimer;

        // Phase 2 Simulation Fields
        private System.Windows.Threading.DispatcherTimer? _simTimer;
        private double _speedRatio = 1.0;
        private bool _isKeplerMode = false;
        private bool _showLabels = false;

        private class PlanetRuntime
        {
            public Planet Data { get; }
            public Grid Node { get; }
            public RotateTransform SelfTransform { get; }
            public TextBlock LabelNode { get; }
            public double OrbitRadius { get; }
            public double Eccentricity { get; }
            
            public double OrbitAngle { get; set; } // theta in degrees
            public double SelfAngle { get; set; }  // self-rotation angle in degrees
            
            public List<MoonRuntime> Moons { get; } = new();

            public PlanetRuntime(Planet data, Grid node, RotateTransform selfTransform, TextBlock labelNode, double orbitRadius, double eccentricity)
            {
                Data = data;
                Node = node;
                SelfTransform = selfTransform;
                LabelNode = labelNode;
                OrbitRadius = orbitRadius;
                Eccentricity = eccentricity;
                
                // Deterministic start position based on name hash
                OrbitAngle = System.Math.Abs(data.Name.GetHashCode() % 360);
                SelfAngle = 0;
            }
        }

        private class MoonRuntime
        {
            public UIElement MoonShape { get; }
            public double OrbitRadius { get; }
            public double PeriodSeconds { get; }
            public double Angle { get; set; }

            public MoonRuntime(UIElement moonShape, double orbitRadius, double periodSeconds)
            {
                MoonShape = moonShape;
                OrbitRadius = orbitRadius;
                PeriodSeconds = periodSeconds;
                Angle = new Random().NextDouble() * 360.0;
            }
        }

        private readonly List<PlanetRuntime> _activePlanets = new();

        public Canvas SolarSystemCanvasNode => SolarSystemCanvas;

        public PlanetsTool()
        {
            InitializeComponent();
            Loaded += PlanetsTool_Loaded;
            Unloaded += PlanetsTool_Unloaded;
        }

        private void PlanetsTool_Loaded(object sender, RoutedEventArgs e)
        {
            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
            menuTextOrbit.Text = isVN ? "Hệ Mặt Trời" : "Solar System";
            menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Practical Apps";
            
            sideMenu.SelectedIndex = 0;

            _loadedPlanets = LoadPlanetsFromDb();
            BuildPlanetCards();
            LoadPracticalApps();
        }

        private void PlanetsTool_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        // ═══════════════════════════════════════════════════════════
        //  PLANET DATA RECORD & DEFAULTS
        // ═══════════════════════════════════════════════════════════

        public record Planet(
            string Name, string NameVi, string Icon, string ColorHex,
            double DiameterKm, double MassEarth, double DistAU,
            double OrbitalDays, double DayHours, int Moons,
            double GravityG, double TempC, string Type, string Facts,
            string TypeEn, string FactsEn);

        private static readonly Planet[] DefaultPlanets =
        {
            new("Mercury", "Sao Thủy", "☿", "#9E9E9E",
                4879, 0.055, 0.39, 88, 1407.6, 0,
                0.38, 167, "Đá", "Nhỏ nhất, gần Mặt Trời nhất, không có vệ tinh",
                "Rocky", "Smallest and closest planet to the Sun, with no moons"),

            new("Venus", "Sao Kim", "♀", "#FF8F00",
                12104, 0.815, 0.72, 225, 5832.5, 0,
                0.91, 464, "Đá", "Nóng nhất, quay ngược chiều, áp suất gấp 90 lần Trái Đất",
                "Rocky", "Hottest planet, rotates backwards, atmospheric pressure is 90x of Earth"),

            new("Earth", "Trái Đất", "🌍", "#1565C0",
                12756, 1.0, 1.0, 365.25, 24, 1,
                1.0, 15, "Đá", "Hành tinh duy nhất có sự sống đã biết",
                "Rocky", "Only known planet to support and maintain life"),

            new("Mars", "Sao Hỏa", "♂", "#D84315",
                6792, 0.107, 1.52, 687, 24.6, 2,
                0.38, -65, "Đá", "Hành tinh đỏ, có 2 vệ tinh Phobos & Deimos",
                "Rocky", "Red planet, has 2 satellites: Phobos and Deimos"),

            new("Jupiter", "Sao Mộc", "♃", "#F9A825",
                142984, 317.8, 5.2, 4333, 9.9, 95,
                2.53, -110, "Khí", "Lớn nhất, Vết Đỏ Lớn, có 95 vệ tinh",
                "Gas", "Largest planet, Great Red Spot, has 95 moons"),

            new("Saturn", "Sao Thổ", "♄", "#FFD54F",
                120536, 95.2, 9.58, 10759, 10.7, 146,
                1.07, -140, "Khí", "Vành đai nổi tiếng, mật độ nhỏ hơn nước",
                "Gas", "Famous ring system, density is less than water"),

            new("Uranus", "Sao Thiên Vương", "♅", "#4DD0E1",
                51118, 14.5, 19.2, 30687, 17.2, 28,
                0.89, -195, "Băng", "Nghiêng 98°, quay ngang, có 13 vành đai",
                "Ice", "Axial tilt of 98 degrees, rotates sideways, has 13 rings"),

            new("Neptune", "Sao Hải Vương", "♆", "#1976D2",
                49528, 17.1, 30.1, 60190, 16.1, 16,
                1.12, -200, "Băng", "Xa nhất, gió mạnh nhất (2100 km/h)",
                "Ice", "Farthest planet from the Sun, strongest winds (2100 km/h)"),
        };

        private static string GetPlanetIcon(string nameEn)
        {
            return nameEn switch
            {
                "Mercury" => "☿",
                "Venus" => "♀",
                "Earth" => "🌍",
                "Mars" => "♂",
                "Jupiter" => "♃",
                "Saturn" => "♄",
                "Uranus" => "♅",
                "Neptune" => "♆",
                _ => "🪐"
            };
        }

        private static double GetPlanetEccentricity(string nameEn)
        {
            return nameEn switch
            {
                "Mercury" => 0.2056,
                "Venus" => 0.0068,
                "Earth" => 0.0167,
                "Mars" => 0.0934,
                "Jupiter" => 0.0484,
                "Saturn" => 0.0541,
                "Uranus" => 0.0472,
                "Neptune" => 0.0086,
                _ => 0.0
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  DATABASE INTEGRATION
        // ═══════════════════════════════════════════════════════════

        public Planet[] LoadPlanetsFromDb()
        {
            var list = new List<Planet>();
            var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;

            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath)!);

                var keyBytes = QASmartClass.Data.DbEncryptionKeyManager.GetOrInitializeKey();
                var hexKey = Convert.ToHexString(keyBytes);
                using (var connection = new SqliteConnection($"Data Source={dbPath};Password={hexKey};Default Timeout=5;"))
                {
                    connection.Open();
                    using (var pragmaCmd = connection.CreateCommand())
                    {
                        pragmaCmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
                        pragmaCmd.ExecuteNonQuery();
                    }

                    // Create table if not exists
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"
                            CREATE TABLE IF NOT EXISTS LearningToolPlanets (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                NameEn TEXT NOT NULL,
                                NameVi TEXT NOT NULL,
                                ColorHex TEXT NOT NULL,
                                DiameterKm REAL NOT NULL,
                                MassEarth REAL NOT NULL,
                                DistAU REAL NOT NULL,
                                OrbitalDays REAL NOT NULL,
                                DayHours REAL NOT NULL,
                                Moons INTEGER NOT NULL,
                                GravityG REAL NOT NULL,
                                TempC REAL NOT NULL,
                                Type TEXT NOT NULL,
                                Facts TEXT NOT NULL,
                                TypeEn TEXT NOT NULL DEFAULT 'Rocky',
                                FactsEn TEXT NOT NULL DEFAULT 'No description'
                            );";
                        cmd.ExecuteNonQuery();
                    }

                    // Check if columns exist (Migration)
                    bool hasTypeEn = false;
                    bool hasFactsEn = false;
                    using (var checkCmd = connection.CreateCommand())
                    {
                        checkCmd.CommandText = "PRAGMA table_info(LearningToolPlanets);";
                        using (var reader = checkCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string colName = reader["name"]?.ToString() ?? "";
                                if (colName == "TypeEn") hasTypeEn = true;
                                if (colName == "FactsEn") hasFactsEn = true;
                            }
                        }
                    }

                    if (!hasTypeEn)
                    {
                        using (var alterCmd = connection.CreateCommand())
                        {
                            alterCmd.CommandText = "ALTER TABLE LearningToolPlanets ADD COLUMN TypeEn TEXT NOT NULL DEFAULT 'Rocky';";
                            alterCmd.ExecuteNonQuery();
                        }
                    }

                    if (!hasFactsEn)
                    {
                        using (var alterCmd = connection.CreateCommand())
                        {
                            alterCmd.CommandText = "ALTER TABLE LearningToolPlanets ADD COLUMN FactsEn TEXT NOT NULL DEFAULT 'No description';";
                            alterCmd.ExecuteNonQuery();
                        }

                        // Update default values for the seeded ones
                        foreach (var p in DefaultPlanets)
                        {
                            using (var updateCmd = connection.CreateCommand())
                            {
                                updateCmd.CommandText = "UPDATE LearningToolPlanets SET TypeEn = $TypeEn, FactsEn = $FactsEn WHERE NameEn = $NameEn;";
                                updateCmd.Parameters.AddWithValue("$TypeEn", p.TypeEn);
                                updateCmd.Parameters.AddWithValue("$FactsEn", p.FactsEn);
                                updateCmd.Parameters.AddWithValue("$NameEn", p.Name);
                                updateCmd.ExecuteNonQuery();
                            }
                        }
                    }

                    // Check if empty
                    bool isEmpty = true;
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM LearningToolPlanets;";
                        var result = cmd.ExecuteScalar();
                        if (result != null && Convert.ToInt32(result) > 0)
                        {
                            isEmpty = false;
                        }
                    }

                    // Seed if empty
                    if (isEmpty)
                    {
                        using (var transaction = connection.BeginTransaction())
                        {
                            foreach (var p in DefaultPlanets)
                            {
                                using (var cmd = connection.CreateCommand())
                                {
                                    cmd.CommandText = @"
                                        INSERT INTO LearningToolPlanets (NameEn, NameVi, ColorHex, DiameterKm, MassEarth, DistAU, OrbitalDays, DayHours, Moons, GravityG, TempC, Type, Facts, TypeEn, FactsEn)
                                        VALUES ($NameEn, $NameVi, $ColorHex, $DiameterKm, $MassEarth, $DistAU, $OrbitalDays, $DayHours, $Moons, $GravityG, $TempC, $Type, $Facts, $TypeEn, $FactsEn);";
                                    cmd.Parameters.AddWithValue("$NameEn", p.Name);
                                    cmd.Parameters.AddWithValue("$NameVi", p.NameVi);
                                    cmd.Parameters.AddWithValue("$ColorHex", p.ColorHex);
                                    cmd.Parameters.AddWithValue("$DiameterKm", p.DiameterKm);
                                    cmd.Parameters.AddWithValue("$MassEarth", p.MassEarth);
                                    cmd.Parameters.AddWithValue("$DistAU", p.DistAU);
                                    cmd.Parameters.AddWithValue("$OrbitalDays", p.OrbitalDays);
                                    cmd.Parameters.AddWithValue("$DayHours", p.DayHours);
                                    cmd.Parameters.AddWithValue("$Moons", p.Moons);
                                    cmd.Parameters.AddWithValue("$GravityG", p.GravityG);
                                    cmd.Parameters.AddWithValue("$TempC", p.TempC);
                                    cmd.Parameters.AddWithValue("$Type", p.Type);
                                    cmd.Parameters.AddWithValue("$Facts", p.Facts);
                                    cmd.Parameters.AddWithValue("$TypeEn", p.TypeEn);
                                    cmd.Parameters.AddWithValue("$FactsEn", p.FactsEn);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            transaction.Commit();
                        }
                    }

                    // Read
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT NameEn, NameVi, ColorHex, DiameterKm, MassEarth, DistAU, OrbitalDays, DayHours, Moons, GravityG, TempC, Type, Facts, TypeEn, FactsEn FROM LearningToolPlanets ORDER BY DistAU ASC;";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string nameEn = reader.GetString(0);
                                list.Add(new Planet(
                                    nameEn,
                                    reader.GetString(1),
                                    GetPlanetIcon(nameEn),
                                    reader.GetString(2),
                                    reader.GetDouble(3),
                                    reader.GetDouble(4),
                                    reader.GetDouble(5),
                                    reader.GetDouble(6),
                                    reader.GetDouble(7),
                                    reader.GetInt32(8),
                                    reader.GetDouble(9),
                                    reader.GetDouble(10),
                                    reader.GetString(11),
                                    reader.GetString(12),
                                    reader.GetString(13),
                                    reader.GetString(14)
                                ));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SQLite load error, fallback to DefaultPlanets: " + ex.Message);
                return DefaultPlanets;
            }

            return list.Count > 0 ? list.ToArray() : DefaultPlanets;
        }

        // ═══════════════════════════════════════════════════════════
        //  REAL-TIME SOLAR SYSTEM PHYSICS ENGINE
        // ═══════════════════════════════════════════════════════════

        private void BuildPlanetCards()
        {
            if (SolarSystemCanvas == null) return;
            
            ClearCanvas();

            // Hide tutorial tooltip after 6 seconds
            if (_tutorialTimer == null)
            {
                _tutorialTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
                _tutorialTimer.Tick += TutorialTimer_Tick;
                _tutorialTimer.Start();
            }

            _activePlanets.Clear();

            double baseOrbitRadius = 70; // Mercury orbit radius
            double orbitSpacing = 35;    // Orbit spacing

            DrawOrbitPaths();

            for (int i = 0; i < _loadedPlanets.Length; i++)
            {
                var p = _loadedPlanets[i];
                double orbitRadius = baseOrbitRadius + (i * orbitSpacing);
                double eccentricity = GetPlanetEccentricity(p.Name);

                // Non-linear but strictly monotonic scaling
                double planetSize = 14;
                if (p.Name == "Jupiter") planetSize = 38;
                else if (p.Name == "Saturn") planetSize = 33;
                else if (p.Name == "Uranus" || p.Name == "Neptune") planetSize = 25;
                else if (p.Name == "Earth" || p.Name == "Venus") planetSize = 18;
                else if (p.Name == "Mars") planetSize = 15;
                else if (p.Name == "Mercury") planetSize = 13;

                double nodeSize = p.Name == "Saturn" ? planetSize * 1.6 : planetSize;
                var planetNode = new Grid
                {
                    Width = nodeSize,
                    Height = nodeSize,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = p
                };

                // Build surface, self rotation transform, and label TextBlock
                BuildPlanetSurface(p, planetNode, planetSize, out var selfTransform, out var labelNode);

                var runtime = new PlanetRuntime(p, planetNode, selfTransform, labelNode, orbitRadius, eccentricity);

                // Add Satellites (Moons)
                AddVisualMoonsRuntime(p, planetNode, planetSize, runtime);

                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                ToolTipService.SetToolTip(planetNode, isVN ? $"{p.NameVi} ({p.Name})" : $"{p.Name} ({p.NameVi})");

                // Set initial position
                Canvas.SetLeft(planetNode, -nodeSize / 2);
                Canvas.SetTop(planetNode, -nodeSize / 2);

                // Interactions
                planetNode.MouseEnter += PlanetNode_MouseEnter;
                planetNode.MouseLeave += PlanetNode_MouseLeave;
                planetNode.MouseLeftButtonUp += PlanetNode_Click;

                SolarSystemCanvas.Children.Add(planetNode);
                _activePlanets.Add(runtime);
            }

            // Start simulation loop
            if (_simTimer == null)
            {
                _simTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Render);
                _simTimer.Interval = TimeSpan.FromMilliseconds(16);
                _simTimer.Tick += SimTimer_Tick;
                _simTimer.Start();
            }

            // Sync CheckBoxes
            if (ChkShowLabels != null) ChkShowLabels.IsChecked = _showLabels;
            if (ChkKeplerMode != null) ChkKeplerMode.IsChecked = _isKeplerMode;

            // Trigger initial placement
            UpdatePlanetPositions(0.0);
        }

        private void DrawOrbitPaths()
        {
            if (SolarSystemCanvas == null) return;

            // Remove existing orbits
            var ellipses = SolarSystemCanvas.Children.OfType<System.Windows.Shapes.Ellipse>()
                               .Where(el => el.Name != "SunNode" && el.Tag == null)
                               .ToArray();
            foreach (var e in ellipses)
            {
                SolarSystemCanvas.Children.Remove(e);
            }

            double baseOrbitRadius = 70;
            double orbitSpacing = 35;

            for (int i = 0; i < _loadedPlanets.Length; i++)
            {
                var p = _loadedPlanets[i];
                double a = baseOrbitRadius + (i * orbitSpacing);
                double e = _isKeplerMode ? GetPlanetEccentricity(p.Name) : 0.0;
                double b = a * System.Math.Sqrt(1 - e * e);
                double c = a * e; // Focus shift

                var orbitRing = new System.Windows.Shapes.Ellipse
                {
                    Width = a * 2,
                    Height = b * 2,
                    Stroke = new SolidColorBrush(Color.FromArgb(40, 140, 158, 255)),
                    StrokeThickness = 1.2,
                    StrokeDashArray = new DoubleCollection { 6, 4 }
                };

                Canvas.SetLeft(orbitRing, -c - a);
                Canvas.SetTop(orbitRing, -b);
                
                // Add before the planets so they are drawn behind
                SolarSystemCanvas.Children.Insert(0, orbitRing);
            }
        }

        private void BuildPlanetSurface(Planet p, Grid planetNode, double planetSize, out RotateTransform selfTransform, out TextBlock labelNode)
        {
            // Base layer: Solid ellipse
            var baseShape = new System.Windows.Shapes.Ellipse
            {
                Width = planetSize,
                Height = planetSize,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(p.ColorHex)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            // Details layer: Grid with details
            var detailsGrid = new Grid
            {
                Width = planetSize,
                Height = planetSize,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Point(0.5, 0.5)
            };
            
            selfTransform = new RotateTransform(0);
            detailsGrid.RenderTransform = selfTransform;
            
            // Add base shape to details grid
            detailsGrid.Children.Add(baseShape);

            // Add visual markings
            if (p.Name == "Earth")
            {
                // Green landmasses
                detailsGrid.Children.Add(new System.Windows.Shapes.Ellipse
                {
                    Width = planetSize * 0.4,
                    Height = planetSize * 0.3,
                    Fill = Brushes.ForestGreen,
                    Margin = new Thickness(-planetSize * 0.2, -planetSize * 0.1, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
                detailsGrid.Children.Add(new System.Windows.Shapes.Ellipse
                {
                    Width = planetSize * 0.35,
                    Height = planetSize * 0.25,
                    Fill = Brushes.ForestGreen,
                    Margin = new Thickness(planetSize * 0.2, planetSize * 0.2, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }
            else if (p.Name == "Jupiter" || p.Name == "Saturn")
            {
                // Horizontal gas bands
                detailsGrid.Children.Add(new System.Windows.Shapes.Rectangle
                {
                    Width = planetSize,
                    Height = planetSize * 0.12,
                    Fill = new SolidColorBrush(Color.FromRgb(215, 125, 60)),
                    Margin = new Thickness(0, -planetSize * 0.2, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
                detailsGrid.Children.Add(new System.Windows.Shapes.Rectangle
                {
                    Width = planetSize,
                    Height = planetSize * 0.08,
                    Fill = new SolidColorBrush(Color.FromRgb(160, 100, 50)),
                    Margin = new Thickness(0, planetSize * 0.15, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }
            else if (p.Name == "Mars" || p.Name == "Mercury")
            {
                // Craters / dark basins
                detailsGrid.Children.Add(new System.Windows.Shapes.Ellipse
                {
                    Width = planetSize * 0.2,
                    Height = planetSize * 0.2,
                    Fill = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0)),
                    Margin = new Thickness(-planetSize * 0.15, -planetSize * 0.15, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
                detailsGrid.Children.Add(new System.Windows.Shapes.Ellipse
                {
                    Width = planetSize * 0.15,
                    Height = planetSize * 0.15,
                    Fill = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0)),
                    Margin = new Thickness(planetSize * 0.2, planetSize * 0.1, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }
            else if (p.Name == "Uranus" || p.Name == "Neptune")
            {
                // Subtle ice bands
                detailsGrid.Children.Add(new System.Windows.Shapes.Rectangle
                {
                    Width = planetSize,
                    Height = planetSize * 0.06,
                    Fill = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)),
                    Margin = new Thickness(0, -planetSize * 0.15, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            planetNode.Children.Add(detailsGrid);

            // Lighting layer: Static overlay (highlight and shadow)
            var lightingOverlay = new System.Windows.Shapes.Ellipse
            {
                Width = planetSize,
                Height = planetSize,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };

            var lightBrush = new RadialGradientBrush
            {
                GradientOrigin = new Point(0.35, 0.35),
                Center = new Point(0.5, 0.5),
                RadiusX = 0.5,
                RadiusY = 0.5
            };
            lightBrush.GradientStops.Add(new GradientStop(Color.FromArgb(70, 255, 255, 255), 0.0)); // Highlight
            lightBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.5));        // Mid
            lightBrush.GradientStops.Add(new GradientStop(Color.FromArgb(180, 0, 0, 0), 1.0));      // Shadow

            lightingOverlay.Fill = lightBrush;
            planetNode.Children.Add(lightingOverlay);

            // Saturn's rings outside of rotating details
            if (p.Name == "Saturn")
            {
                var rings = new System.Windows.Shapes.Ellipse
                {
                    Width = planetSize * 1.6,
                    Height = planetSize * 0.4,
                    Stroke = new SolidColorBrush(Color.FromRgb(210, 180, 140)),
                    StrokeThickness = planetSize * 0.12,
                    RenderTransform = new RotateTransform(-15),
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsHitTestVisible = false
                };
                planetNode.Children.Add(rings);
            }

            // Create Label Node
            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            labelNode = new TextBlock
            {
                Text = isVN ? p.NameVi : p.Name,
                Foreground = new SolidColorBrush(Color.FromRgb(197, 202, 233)),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, -18, 0, 0),
                Visibility = _showLabels ? Visibility.Visible : Visibility.Collapsed,
                FontFamily = Application.Current?.TryFindResource("InterFont") as FontFamily ?? new FontFamily("Segoe UI")
            };
            planetNode.Children.Add(labelNode);
        }

        private void AddVisualMoonsRuntime(Planet p, Grid planetNode, double planetSize, PlanetRuntime runtime)
        {
            var moonsList = new List<(string Name, double OrbitRadius, double Size, double PeriodSeconds, string ColorHex)>();

            if (p.Name == "Earth")
            {
                moonsList.Add(("Moon", planetSize * 0.9, planetSize * 0.22, 2.0, "#ECEFF1"));
            }
            else if (p.Name == "Mars")
            {
                moonsList.Add(("Phobos", planetSize * 0.75, planetSize * 0.16, 1.2, "#CFD8DC"));
                moonsList.Add(("Deimos", planetSize * 1.05, planetSize * 0.12, 2.4, "#B0BEC5"));
            }
            else if (p.Name == "Jupiter")
            {
                moonsList.Add(("Io", planetSize * 0.65, planetSize * 0.11, 1.0, "#FFF59D"));
                moonsList.Add(("Europa", planetSize * 0.8, planetSize * 0.10, 1.5, "#B3E5FC"));
                moonsList.Add(("Ganymede", planetSize * 0.95, planetSize * 0.13, 2.2, "#E0E0E0"));
                moonsList.Add(("Callisto", planetSize * 1.1, planetSize * 0.12, 3.0, "#90A4AE"));
            }
            else if (p.Name == "Saturn")
            {
                moonsList.Add(("Titan", planetSize * 0.95, planetSize * 0.15, 2.5, "#FFE082"));
            }

            foreach (var moon in moonsList)
            {
                // Moon orbit path
                var moonOrbit = new System.Windows.Shapes.Ellipse
                {
                    Width = moon.OrbitRadius * 2,
                    Height = moon.OrbitRadius * 2,
                    Stroke = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255)),
                    StrokeThickness = 0.5,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsHitTestVisible = false
                };
                planetNode.Children.Add(moonOrbit);

                // Moon shape
                var moonBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(moon.ColorHex));
                var moonShape = new System.Windows.Shapes.Ellipse
                {
                    Width = moon.Size,
                    Height = moon.Size,
                    Fill = moonBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsHitTestVisible = false
                };

                // Position on orbit ring
                var transformGroup = new TransformGroup();
                var translateTransform = new TranslateTransform(moon.OrbitRadius, 0);
                var rotateTransform = new RotateTransform(0, 0, 0);
                transformGroup.Children.Add(translateTransform);
                transformGroup.Children.Add(rotateTransform);
                moonShape.RenderTransform = transformGroup;

                planetNode.Children.Add(moonShape);

                // Store in runtime
                runtime.Moons.Add(new MoonRuntime(moonShape, moon.OrbitRadius, moon.PeriodSeconds));
            }
        }

        private void UpdatePlanetPositions(double dt)
        {
            foreach (var pr in _activePlanets)
            {
                double a = pr.OrbitRadius;
                double e = _isKeplerMode ? pr.Eccentricity : 0.0;
                double b = a * System.Math.Sqrt(1 - e * e);
                double c = a * e;

                // 1. Kepler's Second Law angular step modifier
                double angleRad = pr.OrbitAngle * System.Math.PI / 180.0;
                double r = a * (1 - e * e) / (1 + e * System.Math.Cos(angleRad));
                double speedMultiplier = (a / r) * (a / r);

                // Base speed: Earth completes 360 degrees in 10s simulation.
                double baseAngularVelocity = (365.25 / pr.Data.OrbitalDays) * 36.0;
                
                // Adjust velocity for outer planets log scaling (matching the original layout)
                double rawSeconds = (pr.Data.OrbitalDays / 365.25) * 10.0;
                if (rawSeconds > 30.0)
                {
                    double compressedSeconds = 30.0 + System.Math.PI * System.Math.Log(rawSeconds - 20.0) * 6.0;
                    baseAngularVelocity = 360.0 / compressedSeconds;
                }

                if (_selectedPlanetNode != null && pr.Node == _selectedPlanetNode)
                {
                    // Selected planet does not move along orbit
                }
                else
                {
                    pr.OrbitAngle += baseAngularVelocity * speedMultiplier * dt * _speedRatio;
                    pr.OrbitAngle %= 360.0;
                }

                // 2. Calculate Cartesian coordinates
                angleRad = pr.OrbitAngle * System.Math.PI / 180.0;
                r = a * (1 - e * e) / (1 + e * System.Math.Cos(angleRad));
                
                // Origin (0,0) is focus, so x and y are relative to Sun
                double x = r * System.Math.Cos(angleRad);
                double y = r * System.Math.Sin(angleRad);

                Canvas.SetLeft(pr.Node, x - (pr.Node.Width / 2));
                Canvas.SetTop(pr.Node, y - (pr.Node.Height / 2));

                // 3. Self-rotation: Earth day = 24h = 3.0s simulation.
                double rotationDirection = (pr.Data.Name == "Venus" || pr.Data.Name == "Uranus") ? -1.0 : 1.0;
                double baseRotationSpeed = (24.0 / System.Math.Abs(pr.Data.DayHours)) * 120.0;
                
                if (pr.Data.DayHours == 0) baseRotationSpeed = 0;

                pr.SelfAngle += baseRotationSpeed * rotationDirection * dt * _speedRatio;
                pr.SelfAngle %= 360.0;
                pr.SelfTransform.Angle = pr.SelfAngle;

                // 4. Update Moons relative to planet center
                foreach (var moon in pr.Moons)
                {
                    if (_selectedPlanetNode != null && pr.Node == _selectedPlanetNode)
                    {
                        // Selected planet's moons also stop orbiting
                    }
                    else
                    {
                        moon.Angle += (360.0 / moon.PeriodSeconds) * dt * _speedRatio;
                        moon.Angle %= 360.0;
                    }

                    var tg = (TransformGroup)moon.MoonShape.RenderTransform;
                    var rotate = (RotateTransform)tg.Children[1];
                    rotate.Angle = moon.Angle;
                }
            }
        }

        private void SimTimer_Tick(object? sender, EventArgs e)
        {
            if (_isPaused) return;
            UpdatePlanetPositions(0.016);
        }

        // ═══════════════════════════════════════════════════════════
        //  COLOR RENDERING HELPERS
        // ═══════════════════════════════════════════════════════════

        private Brush GetPlanet3DBrush(Planet p)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(p.ColorHex);
                var darkColor = MultiplyColor(color, 0.35);
                var lightColor = ScreenColor(color, 0.6);

                var brush = new RadialGradientBrush
                {
                    GradientOrigin = new Point(0.35, 0.35),
                    Center = new Point(0.5, 0.5),
                    RadiusX = 0.5,
                    RadiusY = 0.5
                };
                brush.GradientStops.Add(new GradientStop(lightColor, 0.0));
                brush.GradientStops.Add(new GradientStop(color, 0.65));
                brush.GradientStops.Add(new GradientStop(darkColor, 1.0));
                return brush;
            }
            catch
            {
                return new SolidColorBrush(Colors.SlateGray);
            }
        }

        private Color MultiplyColor(Color color, double factor)
        {
            return Color.FromRgb(
                (byte)System.Math.Max(0, System.Math.Min(255, color.R * factor)),
                (byte)System.Math.Max(0, System.Math.Min(255, color.G * factor)),
                (byte)System.Math.Max(0, System.Math.Min(255, color.B * factor))
            );
        }

        private Color ScreenColor(Color color, double factor)
        {
            return Color.FromRgb(
                (byte)System.Math.Max(0, System.Math.Min(255, color.R + (255 - color.R) * factor)),
                (byte)System.Math.Max(0, System.Math.Min(255, color.G + (255 - color.G) * factor)),
                (byte)System.Math.Max(0, System.Math.Min(255, color.B + (255 - color.B) * factor))
            );
        }

        // ═══════════════════════════════════════════════════════════
        //  INTERACTION & EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════

        private void PlanetNode_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Grid element && element.Tag is Planet p)
            {
                TutorialTooltip.Visibility = Visibility.Collapsed;

                // Resume outline of previously selected planet
                if (_selectedPlanetNode != null && _selectedPlanetNode != element)
                {
                    var prevOverlay = _selectedPlanetNode.Children.OfType<System.Windows.Shapes.Ellipse>().LastOrDefault();
                    if (prevOverlay != null)
                    {
                        prevOverlay.Stroke = null;
                        prevOverlay.StrokeThickness = 0;
                    }
                }

                _selectedPlanetNode = element;
                var planetOverlay = element.Children.OfType<System.Windows.Shapes.Ellipse>().LastOrDefault();
                if (planetOverlay != null)
                {
                    planetOverlay.StrokeThickness = 3;
                    planetOverlay.Stroke = Brushes.Yellow;
                }
                
                // Show Popup
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                txtPopupIcon.Text = p.Icon;
                txtPopupNameVi.Text = isVN ? p.NameVi : p.Name;
                txtPopupNameEn.Text = isVN ? p.Name : p.NameVi;
                txtPopupFacts.Text = isVN ? $"💡 {p.Facts}" : $"💡 {p.FactsEn}";
                
                txtPopupDiameter.Text = $"{p.DiameterKm:N0} km";
                txtPopupMass.Text = isVN ? $"{p.MassEarth:G4} × Trái Đất" : $"{p.MassEarth:G4} × Earth";
                txtPopupTemp.Text = $"{p.TempC}°C";
                txtPopupOrbit.Text = isVN ? $"{p.OrbitalDays:N0} ngày" : $"{p.OrbitalDays:N0} days";
                txtPopupMoons.Text = $"{p.Moons}";
                txtPopupGravity.Text = isVN ? $"{p.GravityG:F2} g (so với Trái Đất)" : $"{p.GravityG:F2} g (relative to Earth)";
                txtPopupType.Text = isVN ? p.Type : p.TypeEn;

                PlanetDetailPopup.Visibility = Visibility.Visible;
                
                // Zoom animation to center (1.5x zoom)
                var animX = new DoubleAnimation(1.5, TimeSpan.FromSeconds(0.4));
                var animY = new DoubleAnimation(1.5, TimeSpan.FromSeconds(0.4));
                CanvasScale.BeginAnimation(ScaleTransform.ScaleXProperty, animX);
                CanvasScale.BeginAnimation(ScaleTransform.ScaleYProperty, animY);
                
                // Shift system to the left by -240px to leave space for popup
                var panAnimX = new DoubleAnimation(-240, TimeSpan.FromSeconds(0.4));
                CanvasTranslate.BeginAnimation(TranslateTransform.XProperty, panAnimX);
            }
        }

        private void BtnClosePopup_Click(object sender, RoutedEventArgs e)
        {
            PlanetDetailPopup.Visibility = Visibility.Collapsed;
            
            // Reset Zoom
            var animX = new DoubleAnimation(1.0, TimeSpan.FromSeconds(0.4));
            var animY = new DoubleAnimation(1.0, TimeSpan.FromSeconds(0.4));
            CanvasScale.BeginAnimation(ScaleTransform.ScaleXProperty, animX);
            CanvasScale.BeginAnimation(ScaleTransform.ScaleYProperty, animY);

            var panAnimX = new DoubleAnimation(0, TimeSpan.FromSeconds(0.4));
            CanvasTranslate.BeginAnimation(TranslateTransform.XProperty, panAnimX);

            // Resume selected planet
            if (_selectedPlanetNode != null)
            {
                var planetOverlay = _selectedPlanetNode.Children.OfType<System.Windows.Shapes.Ellipse>().LastOrDefault();
                if (planetOverlay != null)
                {
                    planetOverlay.StrokeThickness = 0;
                    planetOverlay.Stroke = null;
                }
                _selectedPlanetNode = null;
            }
        }

        private void BtnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (BtnPlayPause == null) return;
            
            if (_isPaused)
            {
                BtnPlayPause.Content = "Tạm Dừng";
                _isPaused = false;
            }
            else
            {
                BtnPlayPause.Content = "Tiếp Tục";
                _isPaused = true;
            }
        }

        private void SliderSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtSpeedRatio == null) return;
            TxtSpeedRatio.Text = $"{e.NewValue:F1}x";
            _speedRatio = e.NewValue;
        }

        private void ChkShowLabels_Checked(object sender, RoutedEventArgs e)
        {
            _showLabels = true;
            foreach (var pr in _activePlanets)
            {
                if (pr.LabelNode != null)
                {
                    pr.LabelNode.Visibility = Visibility.Visible;
                }
            }
        }

        private void ChkShowLabels_Unchecked(object sender, RoutedEventArgs e)
        {
            _showLabels = false;
            foreach (var pr in _activePlanets)
            {
                if (pr.LabelNode != null)
                {
                    pr.LabelNode.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void ChkKeplerMode_Checked(object sender, RoutedEventArgs e)
        {
            _isKeplerMode = true;
            DrawOrbitPaths();
            UpdatePlanetPositions(0.0);
        }

        private void ChkKeplerMode_Unchecked(object sender, RoutedEventArgs e)
        {
            _isKeplerMode = false;
            DrawOrbitPaths();
            UpdatePlanetPositions(0.0);
        }

        // ═══════════════════════════════════════════════════════════
        //  PRACTICAL APPLICATIONS & IMAGES FALLBACK
        // ═══════════════════════════════════════════════════════════

        private string SafeGetImagePath(string path)
        {
            try
            {
                var uri = new Uri(path, UriKind.RelativeOrAbsolute);
                var stream = Application.GetResourceStream(uri);
                if (stream != null)
                {
                    return path;
                }
            }
            catch
            {
                // Safe fallback to vocab_fallback
            }
            return "pack://application:,,,/QASmartClass;component/Assets/Images/vocab_fallback.png";
        }

        private void LoadPracticalApps()
        {
            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            string suffix = isVN ? "VN" : "EN";

            var items = new System.Collections.Generic.List<PracticalAppItem>
            {
                new PracticalAppItem
                {
                    Icon = "🪐",
                    Title = isVN ? "Chu Kỳ Quỹ Đạo & Định Luật Kepler" : "Orbital Periods & Kepler's Laws",
                    ImagePath = SafeGetImagePath($"pack://application:,,,/QASmartClass;component/Assets/Images/app_planets_1_{suffix}.png"),
                    Description = isVN 
                        ? "Mô phỏng chính xác chu kỳ quay của các hành tinh quanh Mặt Trời. Học sinh hiểu được mối liên hệ giữa khoảng cách bán trục lớn (AU) và thời gian hoàn thành một năm hành tinh." 
                        : "Accurately simulate planetary revolution periods around the Sun, helping students understand the relationship between semi-major axis distance (AU) and orbital year duration."
                },
                new PracticalAppItem
                {
                    Icon = "🌍",
                    Title = isVN ? "Khảo Sát Đặc Tính Vật Lý & Sự Sống" : "Physical Characteristics & Habitability",
                    ImagePath = SafeGetImagePath($"pack://application:,,,/QASmartClass;component/Assets/Images/app_planets_2_{suffix}.png"),
                    Description = isVN 
                        ? "So sánh trực quan các thông số đường kính, khối lượng, nhiệt độ trung bình và số lượng vệ tinh. Phân tích các điều kiện cần thiết để hình thành và duy trì sự sống." 
                        : "Visually compare diameter, mass, average temperature, and moon counts to analyze key requirements for supporting and maintaining alien life."
                },
                new PracticalAppItem
                {
                    Icon = "⚖️",
                    Title = isVN ? "Mô Phỏng Trọng Lực Vũ Trụ & Trọng Lượng" : "Cosmic Gravity & Weight Simulation",
                    ImagePath = SafeGetImagePath($"pack://application:,,,/QASmartClass;component/Assets/Images/app_planets_3_{suffix}.png"),
                    Description = isVN 
                        ? "Tìm hiểu gia tốc trọng trường (g) khác nhau trên mỗi hành tinh. Tính toán trọng lượng của một vật thể hoặc con người khi đặt chân lên bề mặt sao Hỏa, sao Mộc." 
                        : "Study the varying gravitational acceleration (g) across planets to calculate how much an object or human would weigh on Mars or Jupiter."
                }
            };

            try
            {
                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for PlanetsTool: {Err}", ex.Message);
            }
        }

        private void TutorialTimer_Tick(object? sender, EventArgs e)
        {
            if (TutorialTooltip != null)
            {
                TutorialTooltip.Visibility = Visibility.Collapsed;
            }
            if (_tutorialTimer != null)
            {
                _tutorialTimer.Stop();
                _tutorialTimer.Tick -= TutorialTimer_Tick;
                _tutorialTimer = null;
            }
        }

        private void PlanetNode_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Grid planetNode)
            {
                if (planetNode == _selectedPlanetNode) return;
                
                var lightingOverlay = planetNode.Children.OfType<System.Windows.Shapes.Ellipse>().LastOrDefault();
                if (lightingOverlay != null)
                {
                    lightingOverlay.Stroke = Brushes.Yellow;
                    lightingOverlay.StrokeThickness = 2.5;
                }
            }
        }

        private void PlanetNode_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Grid planetNode)
            {
                if (planetNode == _selectedPlanetNode) return;
                
                var lightingOverlay = planetNode.Children.OfType<System.Windows.Shapes.Ellipse>().LastOrDefault();
                if (lightingOverlay != null)
                {
                    lightingOverlay.Stroke = null;
                    lightingOverlay.StrokeThickness = 0;
                }
            }
        }

        private void ClearCanvas()
        {
            if (SolarSystemCanvas == null) return;

            var children = SolarSystemCanvas.Children.Cast<UIElement>().ToArray();
            foreach (var child in children)
            {
                if (child is FrameworkElement fe && fe.Name == "SunNode")
                {
                    continue;
                }

                if (child is Grid planetNode)
                {
                    planetNode.MouseEnter -= PlanetNode_MouseEnter;
                    planetNode.MouseLeave -= PlanetNode_MouseLeave;
                    planetNode.MouseLeftButtonUp -= PlanetNode_Click;
                }

                SolarSystemCanvas.Children.Remove(child);
            }
        }

        public override void Dispose()
        {
            if (_simTimer != null)
            {
                _simTimer.Stop();
                _simTimer.Tick -= SimTimer_Tick;
                _simTimer = null;
            }

            if (_tutorialTimer != null)
            {
                _tutorialTimer.Stop();
                _tutorialTimer.Tick -= TutorialTimer_Tick;
                _tutorialTimer = null;
            }

            _activePlanets.Clear();
            _selectedPlanetNode = null;
            ClearCanvas();
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (rootGrid == null || sideMenu == null || viewGuide == null || viewPractice == null || viewPractical == null) return;
            
            viewGuide.Visibility = Visibility.Collapsed;
            viewPractice.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;
            
            int index = sideMenu.SelectedIndex;
            if (index == 0)
            {
                viewGuide.Visibility = Visibility.Visible;
            }
            else if (index == 1)
            {
                viewPractice.Visibility = Visibility.Visible;
            }
            else if (index == 2)
            {
                viewPractical.Visibility = Visibility.Visible;
            }
        }
    }
}