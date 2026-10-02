using System;
using System.Globalization;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;
using System.Windows.Threading;

namespace QASmartTouch.Forms
{
    public partial class Form2_16_ProtractorTool : Window
    {
        #region Fields

        // Protractor State
        private ProtractorState _state;

        // Drag State for Window
        private bool _isDragging = false;
        private Point _dragStartPoint;

        // Drag State for Rays
        private bool _isDraggingRayA = false;
        private bool _isDraggingRayB = false;

        // Snap Mode
        private SnapMode _currentSnapMode = SnapMode.None;

        // Flip State
        private bool _isFlipped = false;

        // Center Point (Canvas coordinates)
        private Point _centerPoint;

        // Radius
        private double _radius = 200;

        #endregion

        #region Constructor

        public Form2_16_ProtractorTool()
        {
            InitializeComponent();
            // QC_4.2_TOUCH_PIPELINE: STEM Window — WPF tự cô lập, KHÔNG cần ApplyTouchIsolation
            QASmartTouch.Helpers.TouchActivationHelper.ApplyToWindow(this); // QC_4.2_TOUCH_ACTIVATION: Fix "nhấn 2 lần mới kéo được" trên IFP

            // Initialize state with default angles
            _state = new ProtractorState
            {
                CenterX = 220,
                CenterY = 230,
                Radius = 200,
                RayAAngle = 0,
                RayBAngle = 45
            };

            _centerPoint = new Point(_state.CenterX, _state.CenterY);
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Center window on screen
            var workArea = SystemParameters.WorkArea;
            this.Left = (workArea.Width - this.Width) / 2;
            this.Top = (workArea.Height - this.Height) / 2;

            // Draw protractor
            DrawProtractor();

            // Update ray positions
            UpdateRayHandles();

            // Update angle display
            UpdateAngleDisplay();
        }

        #endregion

        #region Drawing Methods

        private void DrawProtractor()
        {
            ProtractorCanvas.Children.Clear();

            // Draw semi-circle arc
            DrawSemiCircle();

            // Draw tick marks and numbers
            DrawTickMarksAndNumbers();

            // Draw rays
            DrawRays();

            // Restore UI elements (handles, angle display, center label)
            RestoreUIElements();
        }

        private void DrawSemiCircle()
        {
            // Draw outer semi-circle
            Path outerArc = new Path
            {
                Stroke = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                StrokeThickness = 2,
                Data = CreateArcGeometry(_centerPoint, _radius, 0, 180)
            };
            ProtractorCanvas.Children.Add(outerArc);

            // Draw baseline
            Line baseline = new Line
            {
                X1 = _centerPoint.X - _radius,
                Y1 = _centerPoint.Y,
                X2 = _centerPoint.X + _radius,
                Y2 = _centerPoint.Y,
                Stroke = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                StrokeThickness = 2
            };
            ProtractorCanvas.Children.Add(baseline);
        }

        private void DrawTickMarksAndNumbers()
        {
            for (int angle = 0; angle <= 180; angle++)
            {
                bool isMajor = angle % 10 == 0;
                bool isMinor = angle % 5 == 0;

                if (!isMajor && !isMinor)
                    continue;

                // Calculate tick position
                double angleRad = DegreeToRadian(angle);
                double tickLength = isMajor ? 15 : 10;
                double outerRadius = _radius;
                double innerRadius = _radius - tickLength;

                Point outerPoint = PolarToCartesian(_centerPoint, outerRadius, angleRad);
                Point innerPoint = PolarToCartesian(_centerPoint, innerRadius, angleRad);

                // Draw tick
                Line tick = new Line
                {
                    X1 = outerPoint.X,
                    Y1 = outerPoint.Y,
                    X2 = innerPoint.X,
                    Y2 = innerPoint.Y,
                    Stroke = isMajor ? Brushes.Black : Brushes.Gray,
                    StrokeThickness = isMajor ? 1.5 : 1
                };
                ProtractorCanvas.Children.Add(tick);

                // Draw numbers for major ticks
                if (isMajor)
                {
                    // Outer scale (0 → 180)
                    Point numberPoint1 = PolarToCartesian(_centerPoint, _radius - 25, angleRad);
                    TextBlock number1 = new TextBlock
                    {
                        Text = angle.ToString(),
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.Black
                    };
                    Canvas.SetLeft(number1, numberPoint1.X - 8);
                    Canvas.SetTop(number1, numberPoint1.Y - 8);
                    ProtractorCanvas.Children.Add(number1);

                    // Inner scale (180 → 0) - flipped
                    int flippedAngle = 180 - angle;
                    Point numberPoint2 = PolarToCartesian(_centerPoint, _radius - 40, angleRad);
                    TextBlock number2 = new TextBlock
                    {
                        Text = flippedAngle.ToString(),
                        FontSize = 9,
                        Foreground = new SolidColorBrush(Color.FromRgb(255, 87, 34)) // Orange
                    };
                    Canvas.SetLeft(number2, numberPoint2.X - 8);
                    Canvas.SetTop(number2, numberPoint2.Y - 8);
                    ProtractorCanvas.Children.Add(number2);
                }
            }
        }

        private void DrawRays()
        {
            // Draw Ray A (from center)
            double rayARadians = DegreeToRadian(_state.RayAAngle);
            Point rayAEnd = PolarToCartesian(_centerPoint, _radius, rayARadians);

            Line rayA = new Line
            {
                X1 = _centerPoint.X,
                Y1 = _centerPoint.Y,
                X2 = rayAEnd.X,
                Y2 = rayAEnd.Y,
                Stroke = new SolidColorBrush(Color.FromRgb(255, 87, 34)), // Orange
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 2 }
            };
            ProtractorCanvas.Children.Add(rayA);

            // Draw Ray B (from center)
            double rayBRadians = DegreeToRadian(_state.RayBAngle);
            Point rayBEnd = PolarToCartesian(_centerPoint, _radius, rayBRadians);

            Line rayB = new Line
            {
                X1 = _centerPoint.X,
                Y1 = _centerPoint.Y,
                X2 = rayBEnd.X,
                Y2 = rayBEnd.Y,
                Stroke = new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Green
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 2 }
            };
            ProtractorCanvas.Children.Add(rayB);

            // Draw arc between rays
            double startAngle = Math.Min(_state.RayAAngle, _state.RayBAngle);
            double endAngle = Math.Max(_state.RayAAngle, _state.RayBAngle);

            Path arcBetweenRays = new Path
            {
                Stroke = new SolidColorBrush(Color.FromArgb(128, 255, 193, 7)), // Semi-transparent yellow
                StrokeThickness = 3,
                Data = CreateArcGeometry(_centerPoint, _radius * 0.5, startAngle, endAngle)
            };
            ProtractorCanvas.Children.Add(arcBetweenRays);
        }

        private void RestoreUIElements()
        {
            // Re-add center label (should be on top)
            Border centerLabel = FindCenterLabel();
            if (centerLabel != null)
            {
                ProtractorCanvas.Children.Remove(centerLabel);
                ProtractorCanvas.Children.Add(centerLabel);
            }

            // Re-add ray handles
            if (ProtractorCanvas.Children.Contains(RayAHandle))
            {
                ProtractorCanvas.Children.Remove(RayAHandle);
            }
            if (ProtractorCanvas.Children.Contains(RayBHandle))
            {
                ProtractorCanvas.Children.Remove(RayBHandle);
            }
            ProtractorCanvas.Children.Add(RayAHandle);
            ProtractorCanvas.Children.Add(RayBHandle);

            // Re-add angle display
            if (ProtractorCanvas.Children.Contains(AngleDisplay))
            {
                ProtractorCanvas.Children.Remove(AngleDisplay);
            }
            ProtractorCanvas.Children.Add(AngleDisplay);
        }

        private Border FindCenterLabel()
        {
            foreach (UIElement element in ProtractorCanvas.Children)
            {
                if (element is Border border)
                {
                    if (border.Child is TextBlock tb && tb.Text == "O")
                    {
                        return border;
                    }
                }
            }
            return null;
        }

        #endregion

        #region Geometry Helpers

        private PathGeometry CreateArcGeometry(Point center, double radius, double startAngleDegrees, double endAngleDegrees)
        {
            double startAngleRad = DegreeToRadian(startAngleDegrees);
            double endAngleRad = DegreeToRadian(endAngleDegrees);

            Point startPoint = PolarToCartesian(center, radius, startAngleRad);
            Point endPoint = PolarToCartesian(center, radius, endAngleRad);

            bool isLargeArc = (endAngleDegrees - startAngleDegrees) > 180;

            PathGeometry pathGeometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure { StartPoint = startPoint };

            ArcSegment arcSegment = new ArcSegment
            {
                Point = endPoint,
                Size = new Size(radius, radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = isLargeArc
            };

            pathFigure.Segments.Add(arcSegment);
            pathGeometry.Figures.Add(pathFigure);

            return pathGeometry;
        }

        private Point PolarToCartesian(Point center, double radius, double angleRadians)
        {
            // Protractor convention: 0° is at 9 o'clock (left), 180° is at 3 o'clock (right)
            // Angles increase counter-clockwise from left to right
            // Standard math uses 0° at 3 o'clock, so we need to flip: angle' = 180° - angle
            double adjustedAngle = Math.PI - angleRadians;
            
            double x = center.X + radius * Math.Cos(adjustedAngle);
            double y = center.Y - radius * Math.Sin(adjustedAngle); // Inverted Y-axis in screen coordinates

            return new Point(x, y);
        }

        private double CartesianToPolar(Point center, Point point)
        {
            double dx = point.X - center.X;
            double dy = center.Y - point.Y; // Inverted Y axis (screen coordinates)

            // Calculate angle in standard math coordinates (0° at right)
            double angleRadians = Math.Atan2(dy, dx);
            if (angleRadians < 0)
                angleRadians += 2 * Math.PI;

            // Convert to protractor coordinates (0° at left, 180° at right)
            // Protractor angle = 180° - Math angle
            angleRadians = Math.PI - angleRadians;
            if (angleRadians < 0)
                angleRadians += 2 * Math.PI;

            double angleDegrees = RadianToDegree(angleRadians);

            // Clamp to 0-180 range for protractor
            if (angleDegrees > 180)
                angleDegrees = 180;
            if (angleDegrees < 0)
                angleDegrees = 0;

            return angleDegrees;
        }

        private double DegreeToRadian(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        private double RadianToDegree(double radians)
        {
            return radians * 180.0 / Math.PI;
        }

        private double ApplySnap(double angle)
        {
            switch (_currentSnapMode)
            {
                case SnapMode.Snap1:
                    return Math.Round(angle);
                case SnapMode.Snap5:
                    return Math.Round(angle / 5) * 5;
                case SnapMode.Snap10:
                    return Math.Round(angle / 10) * 10;
                case SnapMode.Snap15:
                    return Math.Round(angle / 15) * 15;
                default:
                    return angle;
            }
        }

        #endregion

        #region Ray Handle Updates

        private void UpdateRayHandles()
        {
            // Update Ray A handle position
            double rayARadians = DegreeToRadian(_state.RayAAngle);
            Point rayAPos = PolarToCartesian(_centerPoint, _radius, rayARadians);
            Canvas.SetLeft(RayAHandle, rayAPos.X - RayAHandle.Width / 2);
            Canvas.SetTop(RayAHandle, rayAPos.Y - RayAHandle.Height / 2);

            // Update Ray B handle position
            double rayBRadians = DegreeToRadian(_state.RayBAngle);
            Point rayBPos = PolarToCartesian(_centerPoint, _radius, rayBRadians);
            Canvas.SetLeft(RayBHandle, rayBPos.X - RayBHandle.Width / 2);
            Canvas.SetTop(RayBHandle, rayBPos.Y - RayBHandle.Height / 2);
        }

        private void UpdateAngleDisplay()
        {
            double angle = Math.Abs(_state.RayBAngle - _state.RayAAngle);
            if (angle > 180)
                angle = 360 - angle;

            // ✨ NEW: Hiển thị góc chính
            AngleInput.Text = $"{angle:F1}°";
            
            // ✨ NEW: Hiển thị góc bù
            double complementAngle = 180 - angle;
            ComplementText.Text = $"(Bù: {complementAngle:F1}°)";

            string snapText = _currentSnapMode switch
            {
                SnapMode.Snap1 => "Snap: 1°",
                SnapMode.Snap5 => "Snap: 5°",
                SnapMode.Snap10 => "Snap: 10°",
                SnapMode.Snap15 => "Snap: 15°",
                _ => "Snap: OFF"
            };
            SnapModeText.Text = snapText;
        }

        #endregion

        #region Ray Handle Events

        private void RayAHandle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingRayA = true;
            RayAHandle.CaptureMouse();
            e.Handled = true;
        }

        private void RayAHandle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingRayA = false;
            RayAHandle.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void RayAHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingRayA && RayAHandle.IsMouseCaptured)
            {
                Point mousePos = e.GetPosition(ProtractorCanvas);
                double angle = CartesianToPolar(_centerPoint, mousePos);
                angle = ApplySnap(angle);

                _state.RayAAngle = angle;

                DrawProtractor();
                UpdateRayHandles();
                UpdateAngleDisplay();

                e.Handled = true;
            }
        }

        private int? _rayATouchId = null;
        private void RayAHandle_TouchDown(object sender, TouchEventArgs e)
        {
            _isDraggingRayA = true;
            _rayATouchId = e.TouchDevice.Id;
            RayAHandle.CaptureTouch(e.TouchDevice);
            e.Handled = true;
        }

        private void RayAHandle_TouchUp(object sender, TouchEventArgs e)
        {
            if (_rayATouchId == e.TouchDevice.Id)
            {
                _isDraggingRayA = false;
                _rayATouchId = null;
                if (e.TouchDevice.Captured == RayAHandle)
                    RayAHandle.ReleaseTouchCapture(e.TouchDevice);
                e.Handled = true;
            }
        }

        private void RayAHandle_TouchMove(object sender, TouchEventArgs e)
        {
            if (_isDraggingRayA && _rayATouchId == e.TouchDevice.Id)
            {
                Point touchPos = e.GetTouchPoint(ProtractorCanvas).Position;
                double angle = CartesianToPolar(_centerPoint, touchPos);
                angle = ApplySnap(angle);

                _state.RayAAngle = angle;

                DrawProtractor();
                UpdateRayHandles();
                UpdateAngleDisplay();

                e.Handled = true;
            }
        }

        private void RayBHandle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingRayB = true;
            RayBHandle.CaptureMouse();
            e.Handled = true;
        }

        private void RayBHandle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingRayB = false;
            RayBHandle.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void RayBHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingRayB && RayBHandle.IsMouseCaptured)
            {
                Point mousePos = e.GetPosition(ProtractorCanvas);
                double angle = CartesianToPolar(_centerPoint, mousePos);
                angle = ApplySnap(angle);

                _state.RayBAngle = angle;

                DrawProtractor();
                UpdateRayHandles();
                UpdateAngleDisplay();

                e.Handled = true;
            }
        }

        private int? _rayBTouchId = null;
        private void RayBHandle_TouchDown(object sender, TouchEventArgs e)
        {
            _isDraggingRayB = true;
            _rayBTouchId = e.TouchDevice.Id;
            RayBHandle.CaptureTouch(e.TouchDevice);
            e.Handled = true;
        }

        private void RayBHandle_TouchUp(object sender, TouchEventArgs e)
        {
            if (_rayBTouchId == e.TouchDevice.Id)
            {
                _isDraggingRayB = false;
                _rayBTouchId = null;
                if (e.TouchDevice.Captured == RayBHandle)
                    RayBHandle.ReleaseTouchCapture(e.TouchDevice);
                e.Handled = true;
            }
        }

        private void RayBHandle_TouchMove(object sender, TouchEventArgs e)
        {
            if (_isDraggingRayB && _rayBTouchId == e.TouchDevice.Id)
            {
                Point touchPos = e.GetTouchPoint(ProtractorCanvas).Position;
                double angle = CartesianToPolar(_centerPoint, touchPos);
                angle = ApplySnap(angle);

                _state.RayBAngle = angle;

                DrawProtractor();
                UpdateRayHandles();
                UpdateAngleDisplay();

                e.Handled = true;
            }
        }

        #endregion

        #region Button Events - Move

        private void btnMove_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left || e.LeftButton == MouseButtonState.Pressed)
            {
                _isDragging = true;
                _dragStartPoint = PointToScreen(e.GetPosition(this));

                if (sender is UIElement el)
                    el.CaptureMouse();
                e.Handled = true;
            }
        }

        private void btnMove_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentScreenPoint = PointToScreen(e.GetPosition(this));
                double offsetX = currentScreenPoint.X - _dragStartPoint.X;
                double offsetY = currentScreenPoint.Y - _dragStartPoint.Y;

                this.Left += offsetX;
                this.Top += offsetY;

                _dragStartPoint = currentScreenPoint;
                e.Handled = true;
            }
        }

        private void btnMove_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                if (sender is UIElement el)
                    el.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        // ============ TOUCH MOVE HANDLERS (QC_4.2_TOUCH_PIPELINE) ============
        private int? _protractorTouchId = null;

        private void btnMove_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            if (sender is UIElement el)
            {
                _protractorTouchId = e.TouchDevice.Id;
                _isDragging = true;
                _dragStartPoint = PointToScreen(e.GetTouchPoint(this).Position);

                el.CaptureTouch(e.TouchDevice);
                e.Handled = true;
            }
        }

        private void btnMove_TouchMove(object sender, TouchEventArgs e)
        {
            if (_isDragging && _protractorTouchId == e.TouchDevice.Id)
            {
                var currentScreenPoint = PointToScreen(e.GetTouchPoint(this).Position);
                double offsetX = currentScreenPoint.X - _dragStartPoint.X;
                double offsetY = currentScreenPoint.Y - _dragStartPoint.Y;

                this.Left += offsetX;
                this.Top += offsetY;

                _dragStartPoint = currentScreenPoint;
                e.Handled = true;
            }
        }

        private void btnMove_TouchUp(object sender, TouchEventArgs e)
        {
            if (_protractorTouchId == e.TouchDevice.Id)
            {
                _isDragging = false;
                _protractorTouchId = null;

                if (sender is UIElement el && e.TouchDevice.Captured == el)
                    el.ReleaseTouchCapture(e.TouchDevice);

                e.Handled = true;
            }
        }

        private void btnMove_LostTouchCapture(object sender, TouchEventArgs e)
        {
            if (_protractorTouchId == e.TouchDevice.Id)
            {
                _isDragging = false;
                _protractorTouchId = null;
            }
        }

        #endregion

        #region Button Events - Other

        private void btnSnap_Click(object sender, RoutedEventArgs e)
        {
            // Cycle through snap modes
            _currentSnapMode = _currentSnapMode switch
            {
                SnapMode.None => SnapMode.Snap1,
                SnapMode.Snap1 => SnapMode.Snap5,
                SnapMode.Snap5 => SnapMode.Snap10,
                SnapMode.Snap10 => SnapMode.Snap15,
                SnapMode.Snap15 => SnapMode.None,
                _ => SnapMode.None
            };

            UpdateAngleDisplay();
            ShowTemporaryMessage($"Snap mode: {_currentSnapMode}");
        }

        private void btnFlip_Click(object sender, RoutedEventArgs e)
        {
            _isFlipped = !_isFlipped;
            ProtractorScale.ScaleY = _isFlipped ? -1 : 1;
            ShowTemporaryMessage(_isFlipped ? "Flipped (Inner scale active)" : "Normal (Outer scale active)");
        }

        private void btnPin_Click(object sender, RoutedEventArgs e)
        {
            // Create drawing object and send to dashboard
            double angle = Math.Abs(_state.RayBAngle - _state.RayAAngle);
            if (angle > 180)
                angle = 360 - angle;

            // TODO: Implement integration with MainDashboard.AddDrawingObject()
            ShowTemporaryMessage($"Pinned: {angle:F1}° measurement");

            MessageBox.Show(
                $"Angle Measurement: {angle:F1}°\n" +
                $"Ray A: {_state.RayAAngle:F1}°\n" +
                $"Ray B: {_state.RayBAngle:F1}°\n\n" +
                "This result will be added to the dashboard.",
                "Pin Result",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Helper Methods

        private void ShowTemporaryMessage(string message)
        {
            // Create temporary message display
            Border messageBox = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(230, 33, 150, 243)),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(15, 8, 15, 8),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 20, 0, 0)
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

        #region Angle Input Events

        /// <summary>
        /// ✨ NEW: Handle keyboard input for angle
        /// </summary>
        private void AngleInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Parse input
                string input = AngleInput.Text.Replace("°", "").Replace("(", "").Replace(")", "").Trim();
                
                if (double.TryParse(input, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double angle))
                {
                    // Clamp to 0-180 range
                    angle = Math.Clamp(angle, 0, 180);
                    
                    // Apply snap if enabled
                    angle = ApplySnap(angle);
                    
                    // Update Ray B angle (Ray A stays at 0)
                    _state.RayBAngle = angle;
                    
                    // Redraw
                    DrawProtractor();
                    UpdateRayHandles();
                    UpdateAngleDisplay();
                    
                    // Move focus away
                    AngleInput.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    
                    ShowTemporaryMessage($"✓ Đã set góc {angle:F1}°");
                }
                else
                {
                    // Invalid input, restore display
                    UpdateAngleDisplay();
                }
                
                e.Handled = true;
            }
        }

        /// <summary>
        /// ✨ NEW: Select all text when focused for easy editing
        /// </summary>
        private void AngleInput_GotFocus(object sender, RoutedEventArgs e)
        {
            AngleInput.SelectAll();
        }

        /// <summary>
        /// ✨ NEW: Restore display format when focus lost
        /// </summary>
        private void AngleInput_LostFocus(object sender, RoutedEventArgs e)
        {
            // Restore proper display format
            UpdateAngleDisplay();
        }

        #endregion

        #region Quick Draw Angle Methods

        private Form2_MainDashboard? _mainDashboard;

        // Initialize MainDashboard reference
        public void SetMainDashboard(Form2_MainDashboard mainDashboard)
        {
            _mainDashboard = mainDashboard;
        }

        // 1. Vẽ góc 30°
        private void btnDraw30_Click(object sender, RoutedEventArgs e)
        {
            DrawAngleLines(30);
        }

        // 2. Vẽ góc 45°
        private void btnDraw45_Click(object sender, RoutedEventArgs e)
        {
            DrawAngleLines(45);
        }

        // 3. Vẽ góc 60°
        private void btnDraw60_Click(object sender, RoutedEventArgs e)
        {
            DrawAngleLines(60);
        }

        // 4. Vẽ góc vuông 90°
        private void btnDraw90_Click(object sender, RoutedEventArgs e)
        {
            DrawAngleLines(90);
        }

        // 5. Vẽ góc tù 120°
        private void btnDraw120_Click(object sender, RoutedEventArgs e)
        {
            DrawAngleLines(120);
        }

        // 6. Vẽ góc tù 150°
        private void btnDraw150_Click(object sender, RoutedEventArgs e)
        {
            DrawAngleLines(150);
        }

        // Vẽ góc hiện tại đang hiển thị trên thước
        private void btnDrawCurrent_Click(object sender, RoutedEventArgs e)
        {
            double angle = Math.Abs(_state.RayBAngle - _state.RayAAngle);
            if (angle > 180) angle = 360 - angle;
            DrawAngleLines(angle);
        }

        // Helper method: Draw angle lines on MainDashboard
        private void DrawAngleLines(double angle)
        {
            if (_mainDashboard == null)
            {
                MessageBox.Show("Không thể vẽ: MainDashboard chưa được khởi tạo.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Set angle for Ray A (fixed at 0°) and Ray B (at target angle)
                double rayAAngle = 0;
                double rayBAngle = angle;

                // Calculate ray endpoints using protractor coordinate system
                double rayLength = 200;
                
                // Use PolarToCartesian to convert protractor angles to screen coordinates
                Point rayAEnd = PolarToCartesian(_centerPoint, rayLength, DegreeToRadian(rayAAngle));
                Point rayBEnd = PolarToCartesian(_centerPoint, rayLength, DegreeToRadian(rayBAngle));

                // Transform coordinates via screen
                Point centerScreen = ProtractorCanvas.PointToScreen(_centerPoint);
                Point rayAEndScreen = ProtractorCanvas.PointToScreen(rayAEnd);
                Point rayBEndScreen = ProtractorCanvas.PointToScreen(rayBEnd);

                Point centerMain = _mainDashboard.MainInteractiveBoard.PointFromScreen(centerScreen);
                Point rayAEndMain = _mainDashboard.MainInteractiveBoard.PointFromScreen(rayAEndScreen);
                Point rayBEndMain = _mainDashboard.MainInteractiveBoard.PointFromScreen(rayBEndScreen);

                // Draw Ray A (base line)
                Line lineA = new Line
                {
                    X1 = centerMain.X,
                    Y1 = centerMain.Y,
                    X2 = rayAEndMain.X,
                    Y2 = rayAEndMain.Y,
                    Stroke = new SolidColorBrush(Color.FromRgb(255, 87, 34)), // Orange
                    StrokeThickness = 2.0,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };
                _mainDashboard.MainInteractiveBoard.Children.Add(lineA);
                _mainDashboard.RecordToolDrawAction(lineA, $"Protractor: Ray A ({angle}°)");

                // Draw Ray B (angle line)
                Line lineB = new Line
                {
                    X1 = centerMain.X,
                    Y1 = centerMain.Y,
                    X2 = rayBEndMain.X,
                    Y2 = rayBEndMain.Y,
                    Stroke = new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Green
                    StrokeThickness = 2.0,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };
                _mainDashboard.MainInteractiveBoard.Children.Add(lineB);
                _mainDashboard.RecordToolDrawAction(lineB, $"Protractor: Ray B ({angle}°)");

                // Update protractor to show the angle
                _state.RayAAngle = rayAAngle;
                _state.RayBAngle = rayBAngle;
                UpdateRayHandles();
                UpdateAngleDisplay();
                
                // ✨ NEW: Redraw rays on protractor canvas
                RedrawRays();

                ShowTemporaryMessage($"✓ Đã vẽ góc {angle}°");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi vẽ góc: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ✨ NEW: Redraw rays and arc on protractor canvas (without clearing everything)
        /// </summary>
        private void RedrawRays()
        {
            // Remove old rays and arc (if any)
            // Find and remove existing rays by checking for Line and Path elements
            var elementsToRemove = new List<UIElement>();
            foreach (var child in ProtractorCanvas.Children)
            {
                if (child is Line line)
                {
                    // Check if it's a ray (has dashed stroke)
                    if (line.StrokeDashArray != null && line.StrokeDashArray.Count > 0)
                    {
                        elementsToRemove.Add(line);
                    }
                }
                else if (child is Path path)
                {
                    // Check if it's the arc between rays
                    if (path.Stroke is SolidColorBrush brush && brush.Color.A == 128)
                    {
                        elementsToRemove.Add(path);
                    }
                }
            }

            foreach (var element in elementsToRemove)
            {
                ProtractorCanvas.Children.Remove(element);
            }

            // Draw Ray A (from center)
            double rayARadians = DegreeToRadian(_state.RayAAngle);
            Point rayAEnd = PolarToCartesian(_centerPoint, _radius, rayARadians);

            Line rayA = new Line
            {
                X1 = _centerPoint.X,
                Y1 = _centerPoint.Y,
                X2 = rayAEnd.X,
                Y2 = rayAEnd.Y,
                Stroke = new SolidColorBrush(Color.FromRgb(255, 87, 34)), // Orange
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 2 }
            };
            ProtractorCanvas.Children.Add(rayA);

            // Draw Ray B (from center)
            double rayBRadians = DegreeToRadian(_state.RayBAngle);
            Point rayBEnd = PolarToCartesian(_centerPoint, _radius, rayBRadians);

            Line rayB = new Line
            {
                X1 = _centerPoint.X,
                Y1 = _centerPoint.Y,
                X2 = rayBEnd.X,
                Y2 = rayBEnd.Y,
                Stroke = new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Green
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 2 }
            };
            ProtractorCanvas.Children.Add(rayB);

            // Draw arc between rays
            double startAngle = Math.Min(_state.RayAAngle, _state.RayBAngle);
            double endAngle = Math.Max(_state.RayAAngle, _state.RayBAngle);

            Path arcBetweenRays = new Path
            {
                Stroke = new SolidColorBrush(Color.FromArgb(128, 255, 193, 7)), // Semi-transparent yellow
                StrokeThickness = 3,
                Data = CreateArcGeometry(_centerPoint, _radius * 0.5, startAngle, endAngle)
            };
            ProtractorCanvas.Children.Add(arcBetweenRays);
        }

        #endregion
    }

    #region Supporting Classes

    public class ProtractorState
    {
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double Radius { get; set; }
        public double RayAAngle { get; set; } // 0-180 degrees
        public double RayBAngle { get; set; } // 0-180 degrees
    }

    public enum SnapMode
    {
        None,
        Snap1,
        Snap5,
        Snap10,
        Snap15
    }

    #endregion
}

