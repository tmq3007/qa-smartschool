using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace QASmartTouch.Forms
{
    public partial class Form2_18_CompassTool : Window
    {
        #region Fields

        // Compass State
        private CompassState _state;

        // Drag State for Window
        private bool _isDragging = false;
        private Point _dragStartPoint;
        private double _originalLeft = 0;
        private double _originalTop = 0;

        // Compass Interaction State
        private bool _isDraggingCenter = false;
        private bool _isDraggingPencilTip = false;
        private Point _centerPosition;
        private double _currentRadius = 70;

        // Drawing Settings
        private Color _drawColor = Colors.Black;
        private double _drawThickness = 2;
        private bool _isFilled = false;
        private bool _isDashed = false;

        #endregion

        #region Constructor

        public Form2_18_CompassTool()
        {
            InitializeComponent();

            // Initialize state
            _state = new CompassState
            {
                CenterX = 130,
                CenterY = 80,
                Radius = 70
            };

            // Setup event handlers for center point
            CenterPoint.MouseLeftButtonDown += CenterPoint_MouseDown;
            CenterPoint.MouseLeftButtonUp += CenterPoint_MouseUp;
            CenterPoint.MouseMove += CenterPoint_MouseMove;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Center window on screen
            var workArea = SystemParameters.WorkArea;
            this.Left = (workArea.Width - this.Width) / 2;
            this.Top = (workArea.Height - this.Height) / 2;

            // Initialize center position
            _centerPosition = new Point(_state.CenterX, _state.CenterY);
            _currentRadius = _state.Radius;

            // Update compass visual
            UpdateCompass();
        }

        #endregion

        #region Drawing Methods

        private void UpdateCompass()
        {
            // TWO-ARM COMPASS (V-shape): 
            // Arm1 (LEFT) = Needle pin at -30° from vertical
            // Arm2 (RIGHT) = Pencil holder at +30° from vertical
            
            double armAngle = 30; // degrees from vertical (creates 60° opening)
            
            // Arm1 (Needle pin - LEFT side)
            double arm1Rad = (90 - armAngle) * Math.PI / 180.0;
            double arm1X = _centerPosition.X - _currentRadius * Math.Sin(armAngle * Math.PI / 180.0);
            double arm1Y = _centerPosition.Y + _currentRadius * Math.Cos(armAngle * Math.PI / 180.0);
            
            Arm1Geometry.StartPoint = _centerPosition;
            Arm1Geometry.EndPoint = new Point(arm1X, arm1Y);
            
            // Arm2 (Pencil holder - RIGHT side)
            double arm2Rad = (90 + armAngle) * Math.PI / 180.0;
            double arm2X = _centerPosition.X + _currentRadius * Math.Sin(armAngle * Math.PI / 180.0);
            double arm2Y = _centerPosition.Y + _currentRadius * Math.Cos(armAngle * Math.PI / 180.0);
            
            Arm2Geometry.StartPoint = _centerPosition;
            Arm2Geometry.EndPoint = new Point(arm2X, arm2Y);
            
            // Update needle pin position (at end of Arm1)
            UpdateNeedlePin(arm1X, arm1Y);
            
            // Update pencil position (at end of Arm2)
            UpdatePencilPosition(arm2X, arm2Y);

            // Update center point
            Canvas.SetLeft(CenterPoint, _centerPosition.X - 5);
            Canvas.SetTop(CenterPoint, _centerPosition.Y - 5);

            // Update radius display
            RadiusValue.Text = Math.Round(_currentRadius, 0).ToString();

            // Update state
            _state.CenterX = _centerPosition.X;
            _state.CenterY = _centerPosition.Y;
            _state.Radius = _currentRadius;
        }
        
        private void UpdateNeedlePin(double x, double y)
        {
            // Needle pin: Small triangle pointing downward
            var needlePoints = new PointCollection
            {
                new Point(x, y + 5),      // Sharp tip
                new Point(x - 4, y),      // Left base
                new Point(x + 4, y)       // Right base
            };
            NeedlePin.Points = needlePoints;
        }
        
        private void UpdatePencilPosition(double x, double y)
        {
            // Pencil body (vertical rectangle)
            Canvas.SetLeft(PencilBody, x - 4);
            Canvas.SetTop(PencilBody, y - 60);
            
            // Eraser (red hemisphere at top)
            Canvas.SetLeft(Eraser, x - 4);
            Canvas.SetTop(Eraser, y - 64);
            
            // Pencil lead (black triangle at bottom)
            var leadPoints = new PointCollection
            {
                new Point(x, y + 6),      // Sharp tip
                new Point(x - 4, y),      // Left base
                new Point(x + 4, y)       // Right base
            };
            PencilLead.Points = leadPoints;
            
            // Interaction area
            Canvas.SetLeft(PencilTip, x - 8);
            Canvas.SetTop(PencilTip, y - 5);
        }

        #endregion
        
        #region Pencil Tip Interaction (Adjust Radius)
        
        private void PencilTip_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingPencilTip = true;
            ((UIElement)sender).CaptureMouse();
            e.Handled = true;
        }
        
        private void PencilTip_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingPencilTip)
            {
                _isDraggingPencilTip = false;
                ((UIElement)sender).ReleaseMouseCapture();
                e.Handled = true;
            }
        }
        
        private void PencilTip_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingPencilTip && e.LeftButton == MouseButtonState.Pressed)
            {
                Point mousePos = e.GetPosition(CompassCanvas);
                
                // Calculate new radius based on mouse distance from center
                double dx = mousePos.X - _centerPosition.X;
                double dy = mousePos.Y - _centerPosition.Y;
                double newRadius = Math.Sqrt(dx * dx + dy * dy);
                
                // Clamp radius (30 to 120 pixels)
                newRadius = Math.Max(30, Math.Min(newRadius, 120));
                
                _currentRadius = newRadius;
                UpdateCompass();
                
                e.Handled = true;
            }
        }

        #endregion



        #region Center Point Interaction

        private void CenterPoint_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingCenter = true;
            ((UIElement)sender).CaptureMouse();
            e.Handled = true;
        }

        private void CenterPoint_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingCenter)
            {
                _isDraggingCenter = false;
                ((UIElement)sender).ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void CenterPoint_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingCenter && e.LeftButton == MouseButtonState.Pressed)
            {
                Point mousePos = e.GetPosition(CompassCanvas);

                // Clamp within canvas
                mousePos.X = Math.Max(40, Math.Min(mousePos.X, 220));
                mousePos.Y = Math.Max(40, Math.Min(mousePos.Y, 220));

                _centerPosition = mousePos;
                UpdateCompass();

                e.Handled = true;
            }
        }

        #endregion

        #region Button Handlers

        private void BtnMove_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _isDragging = true;
                _dragStartPoint = e.GetPosition(this.Owner);
                _originalLeft = this.Left;
                _originalTop = this.Top;
                ((Button)sender).CaptureMouse();
                e.Handled = true;
            }
        }

        private void BtnMove_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                ((Button)sender).ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void BtnMove_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(this.Owner);
                double deltaX = currentPoint.X - _dragStartPoint.X;
                double deltaY = currentPoint.Y - _dragStartPoint.Y;

                this.Left = _originalLeft + deltaX;
                this.Top = _originalTop + deltaY;

                e.Handled = true;
            }
        }



        private void BtnBrush_Click(object sender, RoutedEventArgs e)
        {
            BrushSettingsPopup.IsOpen = !BrushSettingsPopup.IsOpen;
        }







        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region UI Event Handlers







        private void CmbColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbColor.SelectedIndex == 0) _drawColor = Colors.Black;
            else if (cmbColor.SelectedIndex == 1) _drawColor = Colors.Red;
            else if (cmbColor.SelectedIndex == 2) _drawColor = Colors.Blue;
            else if (cmbColor.SelectedIndex == 3) _drawColor = Colors.Green;
            else if (cmbColor.SelectedIndex == 4) _drawColor = Colors.Gold;
            else if (cmbColor.SelectedIndex == 5) _drawColor = Colors.Purple;
            else if (cmbColor.SelectedIndex == 6) _drawColor = Colors.Orange;
            else if (cmbColor.SelectedIndex == 7) _drawColor = Colors.Brown;
        }

        private void SliderThickness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtThickness == null) return;
            _drawThickness = sliderThickness.Value;
            txtThickness.Text = $"{_drawThickness:F0} px";
        }

        private void ChkFill_Changed(object sender, RoutedEventArgs e)
        {
            _isFilled = chkFill.IsChecked == true;
        }

        private void ChkDashed_Changed(object sender, RoutedEventArgs e)
        {
            _isDashed = chkDashed.IsChecked == true;
        }

        #endregion
    }

    #region Supporting Classes

    public class CompassState
    {
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double Radius { get; set; }
    }

    #endregion
}
