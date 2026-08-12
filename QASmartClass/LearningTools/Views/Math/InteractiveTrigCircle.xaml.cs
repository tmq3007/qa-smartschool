using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace QASmartClass.LearningTools.Views.Math
{
    public partial class InteractiveTrigCircle : UserControl
    {
        public static readonly DependencyProperty AngleProperty =
            DependencyProperty.Register("Angle", typeof(double), typeof(InteractiveTrigCircle),
                new FrameworkPropertyMetadata(45.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnAngleChanged));

        public double Angle
        {
            get => (double)GetValue(AngleProperty);
            set => SetValue(AngleProperty, value);
        }

        private bool _isDragging = false;
        private double _currentRadius = 100.0; // 1 unit = dynamic pixels

        public InteractiveTrigCircle()
        {
            InitializeComponent();
            SizeChanged += (s, e) => Redraw();
            Loaded += (s, e) => Redraw();
        }

        public event EventHandler AngleChanged;

        private static void OnAngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is InteractiveTrigCircle circle)
            {
                circle.Redraw();
                circle.AngleChanged?.Invoke(circle, EventArgs.Empty);
            }
        }

        private void Redraw()
        {
            if (!IsLoaded || drawingCanvas == null) return;

            double w = drawingCanvas.ActualWidth > 0 ? drawingCanvas.ActualWidth : 320;
            double h = drawingCanvas.ActualHeight > 0 ? drawingCanvas.ActualHeight : 320;
            double cx = w / 2.0;
            double cy = h / 2.0;

            // Calculate dynamic radius to fit container nicely, leaving margin for labels
            _currentRadius = System.Math.Min(cx, cy) - 35.0;
            if (_currentRadius < 50.0) _currentRadius = 50.0;

            // Update axes
            axisX.X1 = 10; axisX.Y1 = cy; axisX.X2 = w - 10; axisX.Y2 = cy;
            axisY.X1 = cx; axisY.Y1 = 10; axisY.X2 = cx; axisY.Y2 = h - 10;

            // Unit circle
            Canvas.SetLeft(unitCircle, cx - _currentRadius);
            Canvas.SetTop(unitCircle, cy - _currentRadius);
            unitCircle.Width = _currentRadius * 2;
            unitCircle.Height = _currentRadius * 2;

            // Tangent & Cotangent auxiliary axes lines
            lineTanAxis.X1 = cx + _currentRadius; lineTanAxis.Y1 = 10;
            lineTanAxis.X2 = cx + _currentRadius; lineTanAxis.Y2 = h - 10;

            lineCotAxis.X1 = 10; lineCotAxis.Y1 = cy - _currentRadius;
            lineCotAxis.X2 = w - 10; lineCotAxis.Y2 = cy - _currentRadius;

            // Labels 1 and -1 on axes
            Canvas.SetLeft(lblOneX, cx + _currentRadius + 2);
            Canvas.SetTop(lblOneX, cy + 2);

            Canvas.SetLeft(lblNegOneX, cx - _currentRadius - 15);
            Canvas.SetTop(lblNegOneX, cy + 2);

            Canvas.SetLeft(lblOneY, cx + 4);
            Canvas.SetTop(lblOneY, cy - _currentRadius - 14);

            Canvas.SetLeft(lblNegOneY, cx + 4);
            Canvas.SetTop(lblNegOneY, cy + _currentRadius + 2);

            // Compute math coordinates
            double angleRad = Angle * System.Math.PI / 180.0;
            double cos = System.Math.Cos(angleRad);
            double sin = System.Math.Sin(angleRad);

            // Coordinates on screen
            double px = cx + _currentRadius * cos;
            double py = cy - _currentRadius * sin;

            // Ray Line from O to P
            rayLine.X1 = cx; rayLine.Y1 = cy; rayLine.X2 = px; rayLine.Y2 = py;

            // Point P handle
            Canvas.SetLeft(pointP, px - pointP.Width / 2.0);
            Canvas.SetTop(pointP, py - pointP.Height / 2.0);

            Canvas.SetLeft(lblP, px + 10);
            Canvas.SetTop(lblP, py - 18);

            // Cosine Line (horizontal projection on Ox axis)
            lineCos.X1 = cx; lineCos.Y1 = cy; lineCos.X2 = px; lineCos.Y2 = cy;

            // Sine Line (vertical projection from Ox axis to P)
            lineSin.X1 = px; lineSin.Y1 = cy; lineSin.X2 = px; lineSin.Y2 = py;

            // Tangent Line (on x = cx + radius line)
            if (System.Math.Abs(cos) > 1e-10)
            {
                double tanVal = sin / cos;
                double clampedTan = System.Math.Max(-3.0, System.Math.Min(3.0, tanVal));
                lineTan.X1 = cx + _currentRadius;
                lineTan.Y1 = cy;
                lineTan.X2 = cx + _currentRadius;
                lineTan.Y2 = cy - _currentRadius * clampedTan;
                lineTan.Visibility = Visibility.Visible;
            }
            else
            {
                lineTan.Visibility = Visibility.Collapsed;
            }

            // Cotangent Line (on y = cy - radius line)
            if (System.Math.Abs(sin) > 1e-10)
            {
                double cotVal = cos / sin;
                double clampedCot = System.Math.Max(-3.0, System.Math.Min(3.0, cotVal));
                lineCot.X1 = cx;
                lineCot.Y1 = cy - _currentRadius;
                lineCot.X2 = cx + _currentRadius * clampedCot;
                lineCot.Y2 = cy - _currentRadius;
                lineCot.Visibility = Visibility.Visible;
            }
            else
            {
                lineCot.Visibility = Visibility.Collapsed;
            }

            // Draw Angle Arc and label θ
            UpdateArc(cx, cy, cos, sin);

            // Position dynamic labels for trig lines (sin, cos, tan, cot)
            // 1. Cos label (horizontal line on Ox)
            if (System.Math.Abs(cos) > 0.15 && lblCosText != null)
            {
                lblCosText.Visibility = Visibility.Visible;
                Canvas.SetLeft(lblCosText, cx + (_currentRadius * cos) / 2.0 - 10.0);
                Canvas.SetTop(lblCosText, cy + 4.0);
            }
            else if (lblCosText != null)
            {
                lblCosText.Visibility = Visibility.Collapsed;
            }

            // 2. Sin label (vertical line to P)
            if (System.Math.Abs(sin) > 0.15 && lblSinText != null)
            {
                lblSinText.Visibility = Visibility.Visible;
                Canvas.SetLeft(lblSinText, px + (cos >= 0 ? 6.0 : -25.0));
                Canvas.SetTop(lblSinText, cy - (_currentRadius * sin) / 2.0 - 8.0);
            }
            else if (lblSinText != null)
            {
                lblSinText.Visibility = Visibility.Collapsed;
            }

            // 3. Tan label (vertical tangent line)
            if (lineTan.Visibility == Visibility.Visible && System.Math.Abs(sin) > 0.05 && lblTanText != null)
            {
                lblTanText.Visibility = Visibility.Visible;
                double clampedTan = System.Math.Max(-3.0, System.Math.Min(3.0, sin / cos));
                Canvas.SetLeft(lblTanText, cx + _currentRadius + (cos >= 0 ? 6.0 : -26.0));
                Canvas.SetTop(lblTanText, cy - (_currentRadius * clampedTan) / 2.0 - 8.0);
            }
            else if (lblTanText != null)
            {
                lblTanText.Visibility = Visibility.Collapsed;
            }

            // 4. Cot label (horizontal tangent line)
            if (lineCot.Visibility == Visibility.Visible && System.Math.Abs(cos) > 0.05 && lblCotText != null)
            {
                lblCotText.Visibility = Visibility.Visible;
                double clampedCot = System.Math.Max(-3.0, System.Math.Min(3.0, cos / sin));
                Canvas.SetLeft(lblCotText, cx + (_currentRadius * clampedCot) / 2.0 - 10.0);
                Canvas.SetTop(lblCotText, cy - _currentRadius - (sin >= 0 ? 18.0 : -4.0));
            }
            else if (lblCotText != null)
            {
                lblCotText.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateArc(double cx, double cy, double cos, double sin)
        {
            const double ArcRadius = 30.0;
            double angleNorm = Angle % 360.0;
            if (angleNorm < 0) angleNorm += 360.0;

            if (angleNorm < 0.1)
            {
                angleArc.Visibility = Visibility.Collapsed;
                Canvas.SetLeft(lblAngle, cx + 15);
                Canvas.SetTop(lblAngle, cy - 18);
                return;
            }

            angleArc.Visibility = Visibility.Visible;

            var pathGeometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = new Point(cx + ArcRadius, cy) };

            var arc = new ArcSegment
            {
                Size = new Size(ArcRadius, ArcRadius),
                Point = new Point(cx + ArcRadius * cos, cy - ArcRadius * sin),
                SweepDirection = SweepDirection.Counterclockwise,
                IsLargeArc = angleNorm >= 180.0
            };

            figure.Segments.Add(arc);
            figure.Segments.Add(new LineSegment(new Point(cx, cy), true));
            figure.IsClosed = true;

            pathGeometry.Figures.Add(figure);
            angleArc.Data = pathGeometry;

            // Positioning label theta
            double midAngleRad = (angleNorm / 2.0) * System.Math.PI / 180.0;
            double labelRadius = ArcRadius + 14.0;
            Canvas.SetLeft(lblAngle, cx + labelRadius * System.Math.Cos(midAngleRad) - 5.0);
            Canvas.SetTop(lblAngle, cy - labelRadius * System.Math.Sin(midAngleRad) - 10.0);
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            drawingCanvas.CaptureMouse();
            UpdateAngleFromMouse(e.GetPosition(drawingCanvas));
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                UpdateAngleFromMouse(e.GetPosition(drawingCanvas));
            }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                drawingCanvas.ReleaseMouseCapture();
            }
        }

        private void UpdateAngleFromMouse(Point mousePos)
        {
            double w = drawingCanvas.ActualWidth;
            double h = drawingCanvas.ActualHeight;
            double cx = w / 2.0;
            double cy = h / 2.0;

            double dx = mousePos.X - cx;
            double dy = cy - mousePos.Y;

            double rad = System.Math.Atan2(dy, dx);
            double deg = rad * 180.0 / System.Math.PI;

            if (deg < 0) deg += 360.0;

            // Rounded to 1 decimal place for stable values
            Angle = System.Math.Round(deg, 1);
        }
    }
}
