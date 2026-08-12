using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using QASmartTouch.PeriodicTable.Models;

namespace QASmartTouch.PeriodicTable.Views
{
    public partial class SimulationWindow : Window
    {
        private Dictionary<string, Dictionary<string, SolubilityInfo>> solubilityData;
        private Storyboard pourStoryboard;
        private Storyboard precipitateStoryboard;
        
        // Physics simulation for Phương án 2
        private List<PrecipitateParticle> particles;
        private Dictionary<Ellipse, PrecipitateParticle> particleMap;
        private System.Windows.Threading.DispatcherTimer physicsTimer;
        private const double Gravity = 0.5;
        private const double Damping = 0.7;
        private const double BeakerBottom = 125; // Beaker container height (130 - 5 margin)
        private const double BeakerLeft = 5; // Left margin inside beaker
        private const double BeakerRight = 105; // Right boundary (110 - 5 margin)

        public SimulationWindow()
        {
            InitializeComponent();
            LoadSolubilityData();
            
            particles = new List<PrecipitateParticle>();
            particleMap = new Dictionary<Ellipse, PrecipitateParticle>();
            
            // Initialize animations and UI after window is loaded
            this.Loaded += (s, e) =>
            {
                InitializeAnimations();
                UpdatePrediction();
                UpdateLabels();
            };
        }

        private void LoadSolubilityData()
        {
            // Cấu hình chi tiết các phản ứng hóa học (phương trình cân bằng và hiện tượng)
            solubilityData = new Dictionary<string, Dictionary<string, SolubilityInfo>>
            {
                ["Ca²⁺"] = new Dictionary<string, SolubilityInfo>
                {
                    ["CO₃²⁻"] = new SolubilityInfo("CaCO₃", "Không tan", "#FFFFFF", "Ca²⁺ + CO₃²⁻ → CaCO₃↓", "Canxi cacbonat không tan trong nước, tạo kết tủa màu trắng."),
                    ["SO₄²⁻"] = new SolubilityInfo("CaSO₄", "Ít tan", "#F0F0F0", "Ca²⁺ + SO₄²⁻ → CaSO₄↓ (ít tan)", "Canxi sunfat ít tan trong nước, tạo kết tủa nhẹ màu trắng xám."),
                    ["Cl⁻"] = new SolubilityInfo("CaCl₂", "Tan", "", "Ca²⁺ + 2Cl⁻ → CaCl₂", "Canxi clorua tan tốt trong nước, không tạo kết tủa."),
                    ["OH⁻"] = new SolubilityInfo("Ca(OH)₂", "Ít tan", "#FFFFFF", "Ca²⁺ + 2OH⁻ → Ca(OH)₂↓ (ít tan)", "Canxi hiđroxit ít tan trong nước, tạo kết tủa nhẹ màu trắng."),
                    ["PO₄³⁻"] = new SolubilityInfo("Ca₃(PO₄)₂", "Không tan", "#FFFFFF", "3Ca²⁺ + 2PO₄³⁻ → Ca₃(PO₄)₂↓", "Canxi photphat không tan trong nước, tạo kết tủa màu trắng."),
                    ["S²⁻"] = new SolubilityInfo("CaS", "Tan", "", "Ca²⁺ + S²⁻ → CaS", "Canxi sunfua tan tốt trong nước, không tạo kết tủa.")
                },
                ["Ba²⁺"] = new Dictionary<string, SolubilityInfo>
                {
                    ["CO₃²⁻"] = new SolubilityInfo("BaCO₃", "Không tan", "#FFFFFF", "Ba²⁺ + CO₃²⁻ → BaCO₃↓", "Bari cacbonat không tan trong nước, tạo kết tủa màu trắng."),
                    ["SO₄²⁻"] = new SolubilityInfo("BaSO₄", "Không tan", "#FFFFFF", "Ba²⁺ + SO₄²⁻ → BaSO₄↓", "Bari sunfat không tan trong nước, tạo kết tủa màu trắng mịn."),
                    ["Cl⁻"] = new SolubilityInfo("BaCl₂", "Tan", "", "Ba²⁺ + 2Cl⁻ → BaCl₂", "Bari clorua tan tốt trong nước, không tạo kết tủa."),
                    ["OH⁻"] = new SolubilityInfo("Ba(OH)₂", "Tan", "", "Ba²⁺ + 2OH⁻ → Ba(OH)₂", "Bari hiđroxit tan tốt trong nước, không tạo kết tủa."),
                    ["PO₄³⁻"] = new SolubilityInfo("Ba₃(PO₄)₂", "Không tan", "#FFFFFF", "3Ba²⁺ + 2PO₄³⁻ → Ba₃(PO₄)₂↓", "Bari photphat không tan trong nước, tạo kết tủa màu trắng."),
                    ["S²⁻"] = new SolubilityInfo("BaS", "Tan", "", "Ba²⁺ + S²⁻ → BaS", "Bari sunfua tan tốt trong nước, không tạo kết tủa.")
                },
                ["Ag⁺"] = new Dictionary<string, SolubilityInfo>
                {
                    ["CO₃²⁻"] = new SolubilityInfo("Ag₂CO₃", "Không tan", "#F5F5DC", "2Ag⁺ + CO₃²⁻ → Ag₂CO₃↓", "Bạc cacbonat không tan trong nước, tạo kết tủa màu be nhạt."),
                    ["SO₄²⁻"] = new SolubilityInfo("Ag₂SO₄", "Ít tan", "#FFFFFF", "2Ag⁺ + SO₄²⁻ → Ag₂SO₄↓ (ít tan)", "Bạc sunfat ít tan trong nước, tạo kết tủa nhẹ màu trắng."),
                    ["Cl⁻"] = new SolubilityInfo("AgCl", "Không tan", "#FFFFFF", "Ag⁺ + Cl⁻ → AgCl↓", "Bạc clorua không tan trong nước, tạo kết tủa trắng, bị hóa đen ngoài ánh sáng."),
                    ["OH⁻"] = new SolubilityInfo("Ag₂O", "Không tan", "#4A2711", "2Ag⁺ + 2OH⁻ → Ag₂O↓ + H₂O", "Bạc hiđroxit (AgOH) không bền, tự phân hủy tạo thành kết tủa bạc oxit (Ag₂O) màu nâu đen và nước."),
                    ["S²⁻"] = new SolubilityInfo("Ag₂S", "Không tan", "#000000", "2Ag⁺ + S²⁻ → Ag₂S↓", "Bạc sunfua không tan trong nước, tạo kết tủa màu đen."),
                    ["PO₄³⁻"] = new SolubilityInfo("Ag₃PO₄", "Không tan", "#FFEB3B", "3Ag⁺ + PO₄³⁻ → Ag₃PO₄↓", "Bạc photphat không tan trong nước, tạo kết tủa màu vàng.")
                },
                ["Pb²⁺"] = new Dictionary<string, SolubilityInfo>
                {
                    ["CO₃²⁻"] = new SolubilityInfo("PbCO₃", "Không tan", "#FFFFFF", "Pb²⁺ + CO₃²⁻ → PbCO₃↓", "Chì cacbonat không tan trong nước, tạo kết tủa màu trắng."),
                    ["SO₄²⁻"] = new SolubilityInfo("PbSO₄", "Không tan", "#FFFFFF", "Pb²⁺ + SO₄²⁻ → PbSO₄↓", "Chì sunfat không tan trong nước, tạo kết tủa màu trắng."),
                    ["Cl⁻"] = new SolubilityInfo("PbCl₂", "Ít tan", "#FFFFFF", "Pb²⁺ + 2Cl⁻ → PbCl₂↓ (ít tan)", "Chì clorua ít tan trong nước lạnh, tạo kết tủa màu trắng."),
                    ["OH⁻"] = new SolubilityInfo("Pb(OH)₂", "Không tan", "#FFFFFF", "Pb²⁺ + 2OH⁻ → Pb(OH)₂↓", "Chì hiđroxit không tan trong nước, tạo kết tủa màu trắng."),
                    ["S²⁻"] = new SolubilityInfo("PbS", "Không tan", "#000000", "Pb²⁺ + S²⁻ → PbS↓", "Chì sunfua không tan trong nước, tạo kết tủa màu đen."),
                    ["PO₄³⁻"] = new SolubilityInfo("Pb₃(PO₄)₂", "Không tan", "#FFFFFF", "3Pb²⁺ + 2PO₄³⁻ → Pb₃(PO₄)₂↓", "Chì photphat không tan trong nước, tạo kết tủa màu trắng.")
                },
                ["Fe³⁺"] = new Dictionary<string, SolubilityInfo>
                {
                    ["CO₃²⁻"] = new SolubilityInfo("Fe(OH)₃", "Không tan", "#8B4513", "2Fe³⁺ + 3CO₃²⁻ + 3H₂O → 2Fe(OH)₃↓ + 3CO₂↑", "Sắt(III) cacbonat bị thủy phân hoàn toàn trong nước tạo kết tủa nâu đỏ sắt(III) hiđroxit và giải phóng bọt khí CO₂."),
                    ["SO₄²⁻"] = new SolubilityInfo("Fe₂(SO₄)₃", "Tan", "", "2Fe³⁺ + 3SO₄²⁻ → Fe₂(SO₄)₃", "Sắt(III) sunfat tan tốt trong nước, không tạo kết tủa."),
                    ["Cl⁻"] = new SolubilityInfo("FeCl₃", "Tan", "", "Fe³⁺ + 3Cl⁻ → FeCl₃", "Sắt(III) clorua tan tốt trong nước, không tạo kết tủa."),
                    ["OH⁻"] = new SolubilityInfo("Fe(OH)₃", "Không tan", "#8B4513", "Fe³⁺ + 3OH⁻ → Fe(OH)₃↓", "Sắt(III) hiđroxit không tan trong nước, tạo kết tủa màu nâu đỏ."),
                    ["PO₄³⁻"] = new SolubilityInfo("FePO₄", "Không tan", "#8B4513", "Fe³⁺ + PO₄³⁻ → FePO₄↓", "Sắt(III) photphat không tan trong nước, tạo kết tủa màu nâu vàng nhạt."),
                    ["S²⁻"] = new SolubilityInfo("FeS + S", "Không tan", "#000000", "2Fe³⁺ + 3S²⁻ → 2FeS↓ + S↓", "Sắt(III) sunfua không bền trong nước, xảy ra phản ứng oxi hóa - khử tạo kết tủa sắt(II) sunfua màu đen và lưu huỳnh tự do màu vàng nhạt.")
                },
                ["Cu²⁺"] = new Dictionary<string, SolubilityInfo>
                {
                    ["CO₃²⁻"] = new SolubilityInfo("CuCO₃", "Không tan", "#87CEEB", "Cu²⁺ + CO₃²⁻ → CuCO₃↓", "Đồng cacbonat không tan trong nước, tạo kết tủa màu xanh lam nhạt."),
                    ["SO₄²⁻"] = new SolubilityInfo("CuSO₄", "Tan", "", "Cu²⁺ + SO₄²⁻ → CuSO₄", "Đồng sunfat tan tốt trong nước, không tạo kết tủa."),
                    ["Cl⁻"] = new SolubilityInfo("CuCl₂", "Tan", "", "Cu²⁺ + 2Cl⁻ → CuCl₂", "Đồng clorua tan tốt trong nước, không tạo kết tủa."),
                    ["OH⁻"] = new SolubilityInfo("Cu(OH)₂", "Không tan", "#1E90FF", "Cu²⁺ + 2OH⁻ → Cu(OH)₂↓", "Đồng hiđroxit không tan trong nước, tạo kết tủa màu xanh lam."),
                    ["S²⁻"] = new SolubilityInfo("CuS", "Không tan", "#000000", "Cu²⁺ + S²⁻ → CuS↓", "Đồng sunfua không tan trong nước, tạo kết tủa màu đen."),
                    ["PO₄³⁻"] = new SolubilityInfo("Cu₃(PO₄)₂", "Không tan", "#00BCD4", "3Cu²⁺ + 2PO₄³⁻ → Cu₃(PO₄)₂↓", "Đồng photphat không tan trong nước, tạo kết tủa màu xanh lam nhạt.")
                }
            };
        }

        private void InitializeAnimations()
        {
            pourStoryboard = (Storyboard)this.Resources["PourAnimation"];
            precipitateStoryboard = (Storyboard)this.Resources["PrecipitateAnimation"];
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePrediction();
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            if (CationComboBox == null || AnionComboBox == null) return;
            if (CationComboBox.SelectedItem != null && AnionComboBox.SelectedItem != null)
            {
                string cation = ((ComboBoxItem)CationComboBox.SelectedItem).Content.ToString().Split(' ')[0];
                string anion = ((ComboBoxItem)AnionComboBox.SelectedItem).Content.ToString().Split(' ')[0];
                
                if (LeftLabel != null) LeftLabel.Text = cation;
                if (RightLabel != null) RightLabel.Text = anion;
            }
        }

        private void UpdatePrediction()
        {
            if (CationComboBox == null || AnionComboBox == null) return;
            if (CationComboBox.SelectedItem == null || AnionComboBox.SelectedItem == null)
                return;
            if (solubilityData == null) return;

            string cation = ((ComboBoxItem)CationComboBox.SelectedItem).Content.ToString().Split(' ')[0];
            string anion = ((ComboBoxItem)AnionComboBox.SelectedItem).Content.ToString().Split(' ')[0];

            if (solubilityData.ContainsKey(cation) && solubilityData[cation].ContainsKey(anion))
            {
                var info = solubilityData[cation][anion];
                
                if (info.Solubility == "Không tan")
                {
                    if (PredictionText != null) PredictionText.Text = $"✅ Sẽ có kết tủa {info.Product} màu {GetColorName(info.Color).ToLower()}";
                    if (EquationText != null) EquationText.Text = !string.IsNullOrEmpty(info.CustomEquation) ? info.CustomEquation : $"{cation} + {anion} → {info.Product}↓";
                    if (ExplanationText != null) ExplanationText.Text = !string.IsNullOrEmpty(info.CustomExplanation) ? info.CustomExplanation : $"{info.Product} không tan trong nước, tạo kết tủa màu {GetColorName(info.Color).ToLower()}.";
                }
                else if (info.Solubility == "Ít tan")
                {
                    if (PredictionText != null) PredictionText.Text = $"⚠️ Có thể có kết tủa nhẹ {info.Product}";
                    if (EquationText != null) EquationText.Text = !string.IsNullOrEmpty(info.CustomEquation) ? info.CustomEquation : $"{cation} + {anion} → {info.Product}↓ (ít tan)";
                    if (ExplanationText != null) ExplanationText.Text = !string.IsNullOrEmpty(info.CustomExplanation) ? info.CustomExplanation : $"{info.Product} ít tan trong nước, có thể tạo kết tủa nhẹ.";
                }
                else
                {
                    if (PredictionText != null) PredictionText.Text = $"❌ Không có kết tủa. {info.Product} tan hoàn toàn.";
                    if (EquationText != null) EquationText.Text = !string.IsNullOrEmpty(info.CustomEquation) ? info.CustomEquation : $"{cation} + {anion} → {info.Product} (tan)";
                    if (ExplanationText != null) ExplanationText.Text = !string.IsNullOrEmpty(info.CustomExplanation) ? info.CustomExplanation : $"{info.Product} tan hoàn toàn trong nước, không tạo kết tủa.";
                }
            }
            else
            {
                if (PredictionText != null) PredictionText.Text = $"Chưa cấu hình mô phỏng cho {cation} và {anion}";
                if (EquationText != null) EquationText.Text = $"{cation} + {anion} → (Chưa hỗ trợ)";
                if (ExplanationText != null) ExplanationText.Text = $"Cặp ion {cation} và {anion} hiện chưa có dữ liệu mô phỏng chi tiết.";
            }
        }

        private string GetColorName(string hexColor)
        {
            var colorMap = new Dictionary<string, string>
            {
                ["#FFFFFF"] = "Trắng",
                ["#000000"] = "Đen",
                ["#8B4513"] = "Nâu đỏ",
                ["#87CEEB"] = "Xanh lam nhạt",
                ["#1E90FF"] = "Xanh lam",
                ["#F5F5DC"] = "Be nhạt",
                ["#F0F0F0"] = "Trắng xám",
                ["#4A2711"] = "Nâu đen"
            };

            return colorMap.ContainsKey(hexColor) ? colorMap[hexColor] : "Trắng";
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            RunAnimation();
        }

        private void RunAnimation()
        {
            if (CationComboBox == null || AnionComboBox == null || solubilityData == null) return;
            if (CationComboBox.SelectedItem == null || AnionComboBox.SelectedItem == null) return;

            // Reset first
            ResetAnimation();

            string cation = ((ComboBoxItem)CationComboBox.SelectedItem).Content.ToString().Split(' ')[0];
            string anion = ((ComboBoxItem)AnionComboBox.SelectedItem).Content.ToString().Split(' ')[0];

            if (!solubilityData.ContainsKey(cation) || !solubilityData[cation].ContainsKey(anion))
                return;

            var info = solubilityData[cation][anion];

            // Run pour animation (always)
            if (pourStoryboard != null) pourStoryboard.Begin();
            
            // Create falling droplets effect
            CreateFallingDroplets();

            // Only show precipitate if forms
            if (info.Solubility == "Không tan" || info.Solubility == "Ít tan")
            {
                // Generate precipitate particles with solubility info
                GeneratePrecipitateParticles(info.Color, info.Solubility);

                // Run precipitate animation after pour completes
                System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(4); // After pour + droplets
                timer.Tick += (s, args) =>
                {
                    if (precipitateStoryboard != null) precipitateStoryboard.Begin();
                    timer.Stop();
                };
                timer.Start();
            }
        }
        
        private void CreateFallingDroplets()
        {
            if (DropletsCanvas == null) return;
            
            DropletsCanvas.Children.Clear();
            Random random = new Random();

            // Create 15-20 droplets
            for (int i = 0; i < 18; i++)
            {
                Ellipse droplet = new Ellipse
                {
                    Width = random.Next(4, 8),
                    Height = random.Next(6, 10),
                    Fill = new SolidColorBrush(i < 9 ? 
                        Color.FromRgb(100, 181, 246) : // Blue for left tube
                        Color.FromRgb(255, 183, 77)),   // Orange for right tube
                    Opacity = 0.8
                };

                // Start position (from test tubes center)
                double startX = i < 9 ? 90 : 380; // Left tube: 55+35=90, Right: 345+35=380
                double endX = 235; // Beaker center: 175+60=235
                double startY = 150 + random.Next(-20, 20);
                
                Canvas.SetLeft(droplet, startX);
                Canvas.SetTop(droplet, startY);
                
                DropletsCanvas.Children.Add(droplet);

                double duration = 0.8 + random.NextDouble() * 0.4;
                TimeSpan beginTime = TimeSpan.FromSeconds(0.5 + i * 0.05);

                // Animate horizontal movement to beaker center
                var horizontalAnimation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = startX,
                    To = endX,
                    Duration = TimeSpan.FromSeconds(duration),
                    BeginTime = beginTime
                };
                horizontalAnimation.EasingFunction = new System.Windows.Media.Animation.QuadraticEase 
                { 
                    EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn 
                };

                // Animate vertical falling
                var fallAnimation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = startY,
                    To = 350, // Beaker liquid position
                    Duration = TimeSpan.FromSeconds(duration),
                    BeginTime = beginTime
                };
                fallAnimation.EasingFunction = new System.Windows.Media.Animation.QuadraticEase 
                { 
                    EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn 
                };
                
                // Fade out on arrival
                var fadeAnimation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0.8,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.2),
                    BeginTime = TimeSpan.FromSeconds(duration + 0.5 + i * 0.05)
                };

                droplet.BeginAnimation(Canvas.LeftProperty, horizontalAnimation);
                droplet.BeginAnimation(Canvas.TopProperty, fallAnimation);
                droplet.BeginAnimation(OpacityProperty, fadeAnimation);
            }
        }

        private void GeneratePrecipitateParticles(string color, string solubility)
        {
            if (PrecipitateContainer == null) return;
            
            PrecipitateContainer.Items.Clear();
            particles.Clear();
            particleMap.Clear();
            
            Random random = new Random();
            
            // Determine particle behavior based on solubility
            int totalParticles = 45;
            double floatingRatio = 0.0; // Tỷ lệ particles nổi trên mặt dung dịch
            double liquidLevel = 60; // Liquid surface level (from BeakerLiquid height in animation)
            
            if (solubility == "Không tan")
            {
                // Kết tủa mạnh: 40% particles nổi trên mặt, 60% chìm xuống
                floatingRatio = 0.4;
                totalParticles = 50;
            }
            else if (solubility == "Ít tan")
            {
                // Kết tủa nhẹ: 20% particles nổi lơ lửng, 80% chìm xuống
                floatingRatio = 0.2;
                totalParticles = 35;
            }

            // Create particles with physics properties
            for (int i = 0; i < totalParticles; i++)
            {
                double size = random.Next(6, 14);
                
                // Start position: random in top area of beaker (inside bounds)
                double maxX = BeakerRight - BeakerLeft - size;
                double startX = BeakerLeft + random.NextDouble() * maxX;
                double startY = -size - random.NextDouble() * 40; // Start slightly above visible area
                
                // Determine if this particle floats or sinks
                bool isFloating = random.NextDouble() < floatingRatio;
                double targetY = isFloating 
                    ? liquidLevel - size - random.NextDouble() * 15 // Nổi trên/trong dung dịch
                    : BeakerBottom - size; // Chìm xuống đáy
                
                // Create particle model with physics
                var particleModel = new PrecipitateParticle(startX, startY, size, color)
                {
                    VelocityX = (random.NextDouble() - 0.5) * 0.3, // Very small horizontal drift
                    VelocityY = random.NextDouble() * 0.3, // Initial downward velocity
                    RotationSpeed = (random.NextDouble() - 0.5) * 1.5, // Random rotation
                    Opacity = 0.85 + random.NextDouble() * 0.15, // 0.85-1.0
                    IsFloating = isFloating,
                    TargetY = targetY
                };
                
                particles.Add(particleModel);
                
                // Create visual ellipse
                Ellipse particle = new Ellipse
                {
                    Width = size,
                    Height = size,
                    Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                    Opacity = 0, // Start invisible, fade in
                    RenderTransformOrigin = new Point(0.5, 0.5)
                };
                
                // Add rotation transform
                particle.RenderTransform = new RotateTransform(0);
                
                // Add drop shadow for depth
                particle.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 4,
                    ShadowDepth = 2,
                    Opacity = 0.4
                };
                
                Canvas.SetLeft(particle, startX);
                Canvas.SetTop(particle, startY);
                
                PrecipitateContainer.Items.Add(particle);
                particleMap[particle] = particleModel;
            }

            // Generate gas bubbles if the reaction produces gas (Fe3+ + CO32-)
            if (CationComboBox.SelectedItem != null && AnionComboBox.SelectedItem != null)
            {
                string cation = ((ComboBoxItem)CationComboBox.SelectedItem).Content.ToString().Split(' ')[0];
                string anion = ((ComboBoxItem)AnionComboBox.SelectedItem).Content.ToString().Split(' ')[0];
                if (cation == "Fe³⁺" && anion == "CO₃²⁻")
                {
                    int totalBubbles = 35;
                    for (int i = 0; i < totalBubbles; i++)
                    {
                        double size = random.Next(4, 9);
                        double maxX = BeakerRight - BeakerLeft - size;
                        double startX = BeakerLeft + random.NextDouble() * maxX;
                        // Start bubbles distributed from the bottom up to the liquid level
                        double startY = liquidLevel + random.NextDouble() * (BeakerBottom - liquidLevel - size);

                        var bubbleModel = new PrecipitateParticle(startX, startY, size, "#A0E0FF")
                        {
                            VelocityX = (random.NextDouble() - 0.5) * 0.5,
                            VelocityY = -(0.5 + random.NextDouble() * 1.2), // Rising up
                            RotationSpeed = 0,
                            Opacity = 0.5 + random.NextDouble() * 0.4,
                            IsBubble = true,
                            TargetY = liquidLevel - random.NextDouble() * 8
                        };

                        particles.Add(bubbleModel);

                        Ellipse bubbleVisual = new Ellipse
                        {
                            Width = size,
                            Height = size,
                            Fill = new SolidColorBrush(Color.FromArgb(90, 180, 230, 255)), // Glassy light blue bubble
                            Stroke = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)), // White outline
                            StrokeThickness = 1.2,
                            Opacity = 0,
                            RenderTransformOrigin = new Point(0.5, 0.5)
                        };

                        bubbleVisual.RenderTransform = new RotateTransform(0);

                        Canvas.SetLeft(bubbleVisual, startX);
                        Canvas.SetTop(bubbleVisual, startY);

                        PrecipitateContainer.Items.Add(bubbleVisual);
                        particleMap[bubbleVisual] = bubbleModel;
                    }
                }
            }
            
            // Start physics simulation with delay
            System.Windows.Threading.DispatcherTimer startTimer = new System.Windows.Threading.DispatcherTimer();
            startTimer.Interval = TimeSpan.FromSeconds(2.5); // Start after droplets
            startTimer.Tick += (s, args) =>
            {
                StartPhysicsSimulation();
                startTimer.Stop();
            };
            startTimer.Start();
        }
        
        private void StartPhysicsSimulation()
        {
            // Initialize physics timer
            if (physicsTimer != null)
            {
                physicsTimer.Stop();
                physicsTimer = null;
            }
            
            physicsTimer = new System.Windows.Threading.DispatcherTimer();
            physicsTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            physicsTimer.Tick += PhysicsUpdate;
            physicsTimer.Start();
        }
        
        private void PhysicsUpdate(object sender, EventArgs e)
        {
            bool allLanded = true;
            
            foreach (var kvp in particleMap)
            {
                Ellipse visual = kvp.Key;
                PrecipitateParticle particle = kvp.Value;
                
                if (particle.HasLanded)
                    continue;
                
                allLanded = false;
                
                // Fade in effect
                if (particle.IsBubble)
                {
                    if (visual.Opacity < particle.Opacity && particle.Y > particle.TargetY)
                    {
                        visual.Opacity = Math.Min(visual.Opacity + 0.1, particle.Opacity);
                    }
                }
                else
                {
                    if (visual.Opacity < particle.Opacity)
                    {
                        visual.Opacity = Math.Min(visual.Opacity + 0.05, particle.Opacity);
                    }
                }
                
                // Apply forces
                if (particle.IsBubble)
                {
                    // Bubbles rise: accelerate upwards slightly, up to a limit
                    particle.VelocityY = Math.Max(particle.VelocityY - 0.05, -2.5);
                    // Add gentle horizontal sway
                    particle.VelocityX += (new Random().NextDouble() - 0.5) * 0.15;
                    particle.VelocityX = Math.Max(Math.Min(particle.VelocityX, 0.6), -0.6);
                }
                else if (particle.IsFloating)
                {
                    particle.VelocityY += Gravity * 0.3; // Slower fall for floating particles
                }
                else
                {
                    particle.VelocityY += Gravity;
                }
                
                // Update position
                particle.X += particle.VelocityX;
                particle.Y += particle.VelocityY;
                particle.Rotation += particle.RotationSpeed;
                
                // Check if reached target position (floating, bottom, or bubble pop)
                if (particle.IsBubble)
                {
                    if (particle.Y <= particle.TargetY)
                    {
                        // Pop! fade out quickly
                        visual.Opacity -= 0.2;
                        if (visual.Opacity <= 0)
                        {
                            particle.HasLanded = true;
                            visual.Visibility = Visibility.Collapsed;
                        }
                    }
                }
                else if (particle.Y + particle.Size >= particle.TargetY)
                {
                    if (particle.IsFloating)
                    {
                        // Floating particle: oscillate gently at liquid surface
                        if (particle.Y >= particle.TargetY)
                        {
                            particle.Y = particle.TargetY;
                            particle.VelocityY *= -0.2; // Gentle bounce on surface
                            particle.VelocityX *= 0.95; // Slow horizontal drift
                            particle.RotationSpeed *= 0.9;
                            
                            // Add gentle floating motion
                            if (Math.Abs(particle.VelocityY) < 0.1)
                            {
                                particle.VelocityY = (new Random().NextDouble() - 0.5) * 0.2;
                            }
                            
                            if (Math.Abs(particle.VelocityX) < 0.05 && Math.Abs(particle.VelocityY) < 0.1)
                            {
                                particle.HasLanded = true; // Stable floating
                            }
                        }
                    }
                    else
                    {
                        // Sinking particle: find resting height and settle
                        double restHeight = FindRestingHeight(particle);
                        
                        if (particle.Y >= restHeight)
                        {
                            particle.Y = restHeight;
                            particle.VelocityY = 0;
                            particle.VelocityX *= Damping;
                            particle.RotationSpeed *= Damping;
                            
                            if (Math.Abs(particle.VelocityX) < 0.1 && Math.Abs(particle.RotationSpeed) < 0.5)
                            {
                                particle.HasLanded = true;
                                particle.VelocityX = 0;
                                particle.RotationSpeed = 0;
                            }
                        }
                        else
                        {
                            particle.VelocityY *= -Damping * 0.3; // Small bounce
                        }
                    }
                }
                
                // Keep within beaker horizontal bounds
                if (particle.X < BeakerLeft)
                {
                    particle.X = BeakerLeft;
                    particle.VelocityX *= -Damping;
                }
                if (particle.X + particle.Size > BeakerRight)
                {
                    particle.X = BeakerRight - particle.Size;
                    particle.VelocityX *= -Damping;
                }
                
                // Update visual
                Canvas.SetLeft(visual, particle.X);
                Canvas.SetTop(visual, particle.Y);
                ((RotateTransform)visual.RenderTransform).Angle = particle.Rotation;
            }
            
            // Stop simulation when all particles landed
            if (allLanded && physicsTimer != null)
            {
                physicsTimer.Stop();
            }
        }
        
        private double FindRestingHeight(PrecipitateParticle currentParticle)
        {
            double minY = BeakerBottom - currentParticle.Size;
            
            // Check collision with other particles below
            foreach (var particle in particles)
            {
                if (particle == currentParticle) continue;
                if (!particle.HasLanded && particle.Y < currentParticle.Y - 5) continue; // Only check settled or lower particles
                
                // Check horizontal overlap
                bool overlapX = !(currentParticle.X + currentParticle.Size < particle.X || 
                                 currentParticle.X > particle.X + particle.Size);
                
                if (overlapX)
                {
                    // Stack on top of this particle
                    double topOfParticle = particle.Y - currentParticle.Size;
                    if (topOfParticle < minY)
                    {
                        minY = topOfParticle;
                    }
                }
            }
            
            return minY;
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetAnimation();
        }

        private void ResetAnimation()
        {
            // Stop all animations and physics
            if (pourStoryboard != null) pourStoryboard.Stop();
            if (precipitateStoryboard != null) precipitateStoryboard.Stop();
            if (physicsTimer != null)
            {
                physicsTimer.Stop();
                physicsTimer = null;
            }

            // Reset transforms
            if (LeftTubeRotate != null) LeftTubeRotate.Angle = 0;
            if (RightTubeRotate != null) RightTubeRotate.Angle = 0;

            // Reset liquid levels and positions
            if (LeftLiquid != null)
            {
                LeftLiquid.Height = 150;
                Canvas.SetTop(LeftLiquid, 30); // 180 - 150 = 30
            }
            if (RightLiquid != null)
            {
                RightLiquid.Height = 150;
                Canvas.SetTop(RightLiquid, 30); // 180 - 150 = 30
            }
            if (BeakerLiquid != null)
            {
                BeakerLiquid.Height = 0;
                Canvas.SetTop(BeakerLiquid, 135); // Reset to initial position
            }
            
            // Reset wave positions
            if (LeftWave != null) Canvas.SetTop(LeftWave, 30);
            if (RightWave != null) Canvas.SetTop(RightWave, 30);

            // Clear precipitate and physics data
            if (PrecipitateContainer != null)
            {
                PrecipitateContainer.Items.Clear();
                PrecipitateContainer.Opacity = 0;
            }
            particles.Clear();
            particleMap.Clear();
            
            // Clear droplets
            if (DropletsCanvas != null) DropletsCanvas.Children.Clear();
        }
    }

    // Helper class
    public class SolubilityInfo
    {
        public string Product { get; set; }
        public string Solubility { get; set; }
        public string Color { get; set; }
        public string CustomEquation { get; set; }
        public string CustomExplanation { get; set; }

        public SolubilityInfo(string product, string solubility, string color, string customEquation = null, string customExplanation = null)
        {
            Product = product;
            Solubility = solubility;
            Color = color;
            CustomEquation = customEquation;
            CustomExplanation = customExplanation;
        }
    }
}

