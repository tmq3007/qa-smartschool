using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace QASmartTouch.Forms
{
    public partial class Form2_18_CompassTool_2D : Window
    {
        #region Fields

        // Compass State
        private CompassState2D _state;

        // MainDashboard reference
        private Form2_MainDashboard? _mainDashboard;

        // Drawing state
        private bool _isWaitingForClick = false;
        private double _pendingRadius = 50;

        // Drag state for window
        private bool _isDragging = false;
        private Point _dragStartPoint;

        // Stroke settings
        private Color _strokeColor = Colors.Black;
        private double _strokeThickness = 2.0;

        // Leg length constant
        private const double LEG_LENGTH = 150;

        #endregion

        #region Constructor

        public Form2_18_CompassTool_2D()
        {
            InitializeComponent();

            // Initialize state
            _state = new CompassState2D
            {
                Radius = 50,
                LeftLegAngle = -20,
                RightLegAngle = 20,
                LegLength = LEG_LENGTH
            };

            UpdateCompassVisual();
            UpdateInfoDisplay(_state.Radius);
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Center window on screen
            var workArea = SystemParameters.WorkArea;
            this.Left = (workArea.Width - this.Width) / 2;
            this.Top = (workArea.Height - this.Height) / 2;
        }

        #endregion

        #region Center Joint Drag Events

        private void CenterJoint_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isDragging = true;
                _dragStartPoint = e.GetPosition(this);
                ((Border)sender).CaptureMouse();
                e.Handled = true;
            }
        }

        private void CenterJoint_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(this);
                double deltaX = currentPoint.X - _dragStartPoint.X;
                double deltaY = currentPoint.Y - _dragStartPoint.Y;

                // Get screen position
                Point screenStart = this.PointToScreen(new Point(0, 0));
                this.Left = screenStart.X + deltaX;
                this.Top = screenStart.Y + deltaY;

                e.Handled = true;
            }
        }

        private void CenterJoint_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                ((Border)sender).ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        #endregion

        #region Button Events

        private void btnDraw_Click(object sender, RoutedEventArgs e)
        {
            // Parse radius from input
            if (double.TryParse(RadiusInput.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double radius))
            {
                radius = Math.Clamp(radius, 10, 200);
                _pendingRadius = radius;
                
                // Update compass visual
                UpdateCompassVisual();
                
                // Wait for user to click on canvas
                _isWaitingForClick = true;
                ShowTemporaryMessage($"Click vào canvas để vẽ hình tròn bán kính {radius:F0}px");
            }
            else
            {
                ShowTemporaryMessage("⚠️ Vui lòng nhập số hợp lệ");
            }
        }

        private void btnDraw25_Click(object sender, RoutedEventArgs e)
        {
            DrawCircleWithRadius(25);
        }

        private void btnDraw50_Click(object sender, RoutedEventArgs e)
        {
            DrawCircleWithRadius(50);
        }

        private void btnDraw75_Click(object sender, RoutedEventArgs e)
        {
            DrawCircleWithRadius(75);
        }

        private void btnDraw100_Click(object sender, RoutedEventArgs e)
        {
            DrawCircleWithRadius(100);
        }

        private void btnDraw150_Click(object sender, RoutedEventArgs e)
        {
            DrawCircleWithRadius(150);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Radius Input Events

        private void RadiusInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnDraw_Click(sender, e);
                e.Handled = true;
            }
        }

        private void RadiusInput_GotFocus(object sender, RoutedEventArgs e)
        {
            RadiusInput.SelectAll();
        }

        #endregion

        #region Drawing Methods

        private void DrawCircleWithRadius(double radius)
        {
            _pendingRadius = radius;
            RadiusInput.Text = radius.ToString();
            UpdateCompassVisual();
            _isWaitingForClick = true;
            ShowTemporaryMessage($"Click vào canvas để vẽ hình tròn bán kính {radius:F0}px");
        }

        public void DrawCircleAtPosition(Point screenPosition)
        {
            if (!_isWaitingForClick || _mainDashboard == null)
                return;

            try
            {
                // Convert screen position to MainBoard coordinates
                Point centerMain = _mainDashboard.MainInteractiveBoard.PointFromScreen(screenPosition);

                // Create circle (IsHitTestVisible = false & Fill = null for pen write-through)
                var circle = new Ellipse
                {
                    Width = _pendingRadius * 2,
                    Height = _pendingRadius * 2,
                    Stroke = new SolidColorBrush(_strokeColor),
                    StrokeThickness = _strokeThickness,
                    Fill = null,
                    IsHitTestVisible = false,
                    SnapsToDevicePixels = true
                };

                // Position circle
                Canvas.SetLeft(circle, centerMain.X - _pendingRadius);
                Canvas.SetTop(circle, centerMain.Y - _pendingRadius);

                // Add to canvas
                _mainDashboard.MainInteractiveBoard.Children.Add(circle);
                _mainDashboard.RecordToolDrawAction(circle, $"Compass: draw circle (radius={_pendingRadius:F0}px)");

                // Reset state
                _isWaitingForClick = false;

                // Feedback
                ShowTemporaryMessage($"✓ Đã vẽ hình tròn bán kính {_pendingRadius:F0}px");
                System.Media.SystemSounds.Asterisk.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi vẽ hình tròn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Visual Updates

        private void UpdateCompassVisual()
        {
            // Calculate angle from radius
            double angle = CalculateAngleFromRadius(_pendingRadius);

            _state.LeftLegAngle = -angle;
            _state.RightLegAngle = angle;
            _state.Radius = _pendingRadius;

            // Animate leg rotation
            AnimateLegRotation(LeftLegRotate, _state.LeftLegAngle);
            AnimateLegRotation(RightLegRotate, _state.RightLegAngle);

            // Update info display
            UpdateInfoDisplay(_pendingRadius);

            // Update adjustment bar
            UpdateAdjustmentBar();
        }

        private void AnimateLegRotation(RotateTransform transform, double targetAngle)
        {
            var animation = new DoubleAnimation
            {
                To = targetAngle,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            transform.BeginAnimation(RotateTransform.AngleProperty, animation);
        }

        private void UpdateAdjustmentBar()
        {
            // Calculate bar endpoints based on leg angles
            double centerX = 140; // Center of compass
            double centerY = 80;  // Y position of joint

            // Left endpoint
            double leftX = centerX + 10 * Math.Sin(_state.LeftLegAngle * Math.PI / 180);
            double leftY = centerY;

            // Right endpoint
            double rightX = centerX + 10 * Math.Sin(_state.RightLegAngle * Math.PI / 180);
            double rightY = centerY;

            AdjustmentBar.X1 = leftX;
            AdjustmentBar.Y1 = leftY;
            AdjustmentBar.X2 = rightX;
            AdjustmentBar.Y2 = rightY;
        }

        private void UpdateInfoDisplay(double radius)
        {
            RadiusText.Text = $"Bán kính: {radius:F1}px";
            DiameterText.Text = $"Đường kính: {(radius * 2):F1}px";
            CircumferenceText.Text = $"Chu vi: {(2 * Math.PI * radius):F2}px";
            AreaText.Text = $"Diện tích: {(Math.PI * radius * radius):F2}px²";
        }

        #endregion

        #region Helper Methods

        private double CalculateAngleFromRadius(double radius)
        {
            // Radius = LegLength * sin(angle)
            // angle = arcsin(radius / LegLength)
            double ratio = Math.Clamp(radius / LEG_LENGTH, 0, 1);
            double angleRad = Math.Asin(ratio);
            return angleRad * 180 / Math.PI;
        }

        private void ShowTemporaryMessage(string message)
        {
            // Create temporary message display
            Border messageBox = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(230, 33, 150, 243)),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(15, 8, 15, 8),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 20)
            };

            TextBlock text = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            };

            messageBox.Child = text;

            // Add to grid
            MainGrid.Children.Add(messageBox);

            // Remove after 2 seconds
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            timer.Tick += (s, e) =>
            {
                MainGrid.Children.Remove(messageBox);
                timer.Stop();
            };
            timer.Start();
        }

        #endregion

        #region Public Methods

        public void SetMainDashboard(Form2_MainDashboard mainDashboard)
        {
            _mainDashboard = mainDashboard;
        }

        public bool IsWaitingForClick()
        {
            return _isWaitingForClick;
        }

        #endregion
    }

    #region Supporting Classes

    public class CompassState2D
    {
        public double Radius { get; set; }
        public double LeftLegAngle { get; set; }
        public double RightLegAngle { get; set; }
        public double LegLength { get; set; }
    }

    #endregion
}
