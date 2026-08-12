using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace QASmartClass.Classroom.Views.Controls
{
    public partial class DashboardClock : UserControl
    {
        private DispatcherTimer _timer;
        private bool _isAnalog = false;
        private int _themeIndex = 0;

        public DashboardClock()
        {
            InitializeComponent();
            
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _timer.Tick += Timer_Tick;
            
            Loaded += (_, _) => 
            {
                GenerateAnalogNumbers();
                ApplyTheme();
                _timer.Start();
            };
            
            Unloaded += (_, _) => _timer.Stop();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var now = DateTime.Now;

            if (_isAnalog)
            {
                // Smooth second hand
                double secondFraction = now.Millisecond / 1000.0;
                RotSecond.Angle = (now.Second + secondFraction) * 6;
                RotMinute.Angle = (now.Minute + now.Second / 60.0) * 6;
                RotHour.Angle = (now.Hour % 12 + now.Minute / 60.0) * 30;
            }
            else
            {
                txtDigiTime.Text = now.ToString("HH:mm:ss");
                txtDigiDate.Text = now.ToString("dddd, dd/MM/yyyy");
            }
        }

        private void GenerateAnalogNumbers()
        {
            AnaNumbersCanvas.Children.Clear();
            double radius = 55; // Distance from center
            double centerX = 70;
            double centerY = 70;

            for (int i = 1; i <= 12; i++)
            {
                double angle = i * 30 * Math.PI / 180.0;
                double x = centerX + radius * Math.Sin(angle);
                double y = centerY - radius * Math.Cos(angle);

                var txt = new TextBlock
                {
                    Text = i.ToString(),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Black // Will be overridden by theme
                };

                // Center text
                txt.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(txt, x - txt.DesiredSize.Width / 2);
                Canvas.SetTop(txt, y - txt.DesiredSize.Height / 2);
                
                AnaNumbersCanvas.Children.Add(txt);
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsMenu.PlacementTarget = sender as UIElement;
            SettingsMenu.IsOpen = true;
        }

        private void MenuTypeDigital_Click(object sender, RoutedEventArgs e)
        {
            _isAnalog = false;
            ApplyTheme();
        }

        private void MenuTypeAnalog_Click(object sender, RoutedEventArgs e)
        {
            _isAnalog = true;
            ApplyTheme();
        }

        private void MenuTheme_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag is string tagStr && int.TryParse(tagStr, out int idx))
            {
                _themeIndex = idx;
                ApplyTheme();
            }
        }

        private void ApplyTheme()
        {
            DigitalClockPanel.Visibility = _isAnalog ? Visibility.Collapsed : Visibility.Visible;
            AnalogClockPanel.Visibility = _isAnalog ? Visibility.Visible : Visibility.Collapsed;
            
            // Default reset container
            ContainerBorder.Background = Brushes.White;
            ThemeBackground.Background = Brushes.Transparent;
            ThemeBackground.Effect = null;
            txtDigiTime.Effect = null;

            if (!_isAnalog)
            {
                ApplyDigitalTheme();
            }
            else
            {
                ApplyAnalogTheme();
            }
            
            // Force tick to update immediate state
            Timer_Tick(null, EventArgs.Empty);
        }

        private void ApplyDigitalTheme()
        {
            switch (_themeIndex)
            {
                case 0: // Modern Simple
                    ContainerBorder.Background = Brushes.White;
                    txtDigiTime.Foreground = Brushes.Black;
                    txtDigiDate.Foreground = Brushes.Gray;
                    break;
                case 1: // Dark Mode
                    ContainerBorder.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                    txtDigiTime.Foreground = Brushes.White;
                    txtDigiDate.Foreground = Brushes.LightGray;
                    break;
                case 2: // Neon Blue
                    ContainerBorder.Background = Brushes.Black;
                    txtDigiTime.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 255));
                    txtDigiDate.Foreground = new SolidColorBrush(Color.FromRgb(0, 200, 200));
                    txtDigiTime.Effect = new DropShadowEffect { Color = Color.FromRgb(0, 255, 255), BlurRadius = 15, ShadowDepth = 0 };
                    break;
                case 3: // Cyberpunk
                    ContainerBorder.Background = new SolidColorBrush(Color.FromRgb(15, 10, 30));
                    txtDigiTime.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 150));
                    txtDigiDate.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 255));
                    txtDigiTime.Effect = new DropShadowEffect { Color = Color.FromRgb(255, 0, 150), BlurRadius = 15, ShadowDepth = 0 };
                    break;
                case 4: // Elegant Gold
                    ContainerBorder.Background = new SolidColorBrush(Color.FromRgb(25, 25, 25));
                    txtDigiTime.Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0));
                    txtDigiDate.Foreground = new SolidColorBrush(Color.FromRgb(218, 165, 32));
                    break;
                case 5: // Ocean Blue
                    ContainerBorder.Background = new SolidColorBrush(Color.FromRgb(10, 50, 100));
                    txtDigiTime.Foreground = Brushes.White;
                    txtDigiDate.Foreground = new SolidColorBrush(Color.FromRgb(150, 200, 255));
                    break;
                default:
                    _themeIndex = 0;
                    ApplyDigitalTheme();
                    break;
            }
        }

        private void ApplyAnalogTheme()
        {
            SolidColorBrush faceBrush = Brushes.White;
            SolidColorBrush strokeBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            SolidColorBrush numberBrush = Brushes.Black;
            SolidColorBrush hourMinBrush = Brushes.Black;
            SolidColorBrush secBrush = Brushes.Red;
            SolidColorBrush bgBrush = Brushes.White;

            switch (_themeIndex)
            {
                case 0: // Classic
                    // Defaults
                    break;
                case 1: // Dark
                    bgBrush = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                    faceBrush = new SolidColorBrush(Color.FromRgb(40, 40, 40));
                    strokeBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));
                    numberBrush = Brushes.White;
                    hourMinBrush = Brushes.White;
                    secBrush = new SolidColorBrush(Color.FromRgb(0, 200, 255));
                    break;
                case 2: // Gold
                    bgBrush = new SolidColorBrush(Color.FromRgb(20, 20, 20));
                    faceBrush = new SolidColorBrush(Color.FromRgb(10, 10, 10));
                    strokeBrush = new SolidColorBrush(Color.FromRgb(218, 165, 32));
                    numberBrush = new SolidColorBrush(Color.FromRgb(255, 215, 0));
                    hourMinBrush = new SolidColorBrush(Color.FromRgb(255, 215, 0));
                    secBrush = Brushes.White;
                    break;
                case 3: // Neon
                    bgBrush = Brushes.Black;
                    faceBrush = new SolidColorBrush(Color.FromRgb(15, 15, 15));
                    strokeBrush = new SolidColorBrush(Color.FromRgb(57, 255, 20));
                    numberBrush = new SolidColorBrush(Color.FromRgb(57, 255, 20));
                    hourMinBrush = Brushes.White;
                    secBrush = new SolidColorBrush(Color.FromRgb(255, 0, 150));
                    break;
                case 4: // Minimal
                    bgBrush = Brushes.White;
                    faceBrush = Brushes.White;
                    strokeBrush = Brushes.Transparent;
                    numberBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                    hourMinBrush = new SolidColorBrush(Color.FromRgb(50, 50, 50));
                    secBrush = new SolidColorBrush(Color.FromRgb(255, 100, 100));
                    break;
                case 5: // Retro
                    bgBrush = new SolidColorBrush(Color.FromRgb(245, 240, 230));
                    faceBrush = new SolidColorBrush(Color.FromRgb(240, 230, 210));
                    strokeBrush = new SolidColorBrush(Color.FromRgb(139, 69, 19));
                    numberBrush = new SolidColorBrush(Color.FromRgb(101, 67, 33));
                    hourMinBrush = new SolidColorBrush(Color.FromRgb(101, 67, 33));
                    secBrush = new SolidColorBrush(Color.FromRgb(200, 50, 50));
                    break;
            }

            ContainerBorder.Background = bgBrush;
            AnaFace.Fill = faceBrush;
            AnaFace.Stroke = strokeBrush;
            AnaHourHand.Stroke = hourMinBrush;
            AnaMinuteHand.Stroke = hourMinBrush;
            AnaSecondHand.Stroke = secBrush;
            AnaCenter.Fill = hourMinBrush;

            foreach (var child in AnaNumbersCanvas.Children)
            {
                if (child is TextBlock tb)
                {
                    tb.Foreground = numberBrush;
                }
            }
        }
    }
}
