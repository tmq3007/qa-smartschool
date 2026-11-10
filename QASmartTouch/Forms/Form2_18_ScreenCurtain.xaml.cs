using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using QASmartTouch.Helpers;

namespace QASmartTouch.Forms
{
    public partial class Form2_18_ScreenCurtain : Window
    {
        private RevealMode _currentMode = RevealMode.Vertical;
        private double _revealAmount = 0.2; // 20%
        private bool _isDraggingPanel = false;
        private Point _dragStartPoint;
        
        // ✨ PHASE 2: New fields
        private RevealDirection _currentDirection = RevealDirection.FromTop;
        private bool _isFollowingMouse = false;
        private Point _spotlightPosition;
        
        public enum RevealMode
        {
            Vertical,
            Horizontal,
            Spotlight,
            CustomArea
        }
        
        // ✨ PHASE 2: Direction enum
        public enum RevealDirection
        {
            FromTop,
            FromBottom,
            FromLeft,
            FromRight
        }
        
        public Form2_18_ScreenCurtain()
        {
            InitializeComponent();
            TouchActivationHelper.Apply(this);
            
            // Set window to cover entire screen
            this.Left = 0;
            this.Top = 0;
            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight;
            
            // ✨ PHASE 2: Initialize spotlight position (center)
            _spotlightPosition = new Point(this.Width / 2, this.Height / 2);
            
            // Keyboard shortcuts
            this.KeyDown += Window_KeyDown;
            
            // ✨ PHASE 2: Mouse tracking for spotlight
            this.MouseMove += Window_MouseMove;
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyReveal();
            UpdateRevealLabel();
            UpdatePanelVisibility(); // ✨ PHASE 2: Update UI based on mode
        }
        
        #region Opacity Control
        
        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (CurtainOverlay != null)
            {
                CurtainOverlay.Opacity = OpacitySlider.Value;
                
                // Update display
                int percentage = (int)(OpacitySlider.Value * 100);
                
                if (txtOpacity != null) // ✨ Add null check
                {
                    txtOpacity.Text = $"{percentage}%";
                }
                
                if (txtMinimizedInfo != null)
                {
                    txtMinimizedInfo.Text = $"{percentage}%";
                }
            }
        }
        
        #endregion
        
        #region Reveal Mode
        
        private void RevealMode_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (RevealModeCombo == null || RevealModeCombo.SelectedIndex < 0) return;
            
            _currentMode = (RevealMode)RevealModeCombo.SelectedIndex;
            
            // ✨ PHASE 2: Auto-select appropriate direction when mode changes
            if (DirectionCombo != null)
            {
                RevealDirection requiredDirection = GetDefaultDirectionForMode(_currentMode);
                
                // Only change if current direction is incompatible with new mode
                if (ShouldUpdateDirection(_currentMode, _currentDirection))
                {
                    // Temporarily remove event handler to prevent recursive calls
                    DirectionCombo.SelectionChanged -= Direction_Changed;
                    
                    // Switch to appropriate direction
                    DirectionCombo.SelectedIndex = (int)requiredDirection;
                    _currentDirection = requiredDirection;
                    
                    // Re-attach event handler
                    DirectionCombo.SelectionChanged += Direction_Changed;
                }
            }
            
            // Update UI based on mode (with null checks)
            if (RevealSlider != null && RevealLabel != null)
            {
                switch (_currentMode)
                {
                    case RevealMode.Vertical:
                    case RevealMode.Horizontal:
                        RevealSlider.IsEnabled = true;
                        RevealLabel.Visibility = Visibility.Visible;
                        break;
                        
                    case RevealMode.Spotlight:
                        RevealSlider.IsEnabled = true;
                        RevealLabel.Text = "Spotlight Size";
                        RevealLabel.Visibility = Visibility.Visible;
                        break;
                        
                    case RevealMode.CustomArea:
                        RevealSlider.IsEnabled = false;
                        RevealLabel.Visibility = Visibility.Collapsed;
                        MessageBox.Show("Click and drag on the screen to create reveal areas.\nRight-click to remove areas.", 
                                      "Custom Area Mode", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                }
            }
            
            UpdatePanelVisibility(); // ✨ PHASE 2: Show/hide direction/spotlight panels
            ApplyReveal();
        }
        
        /// <summary>
        /// Checks if direction should be updated when mode changes
        /// </summary>
        private bool ShouldUpdateDirection(RevealMode mode, RevealDirection direction)
        {
            switch (mode)
            {
                case RevealMode.Vertical:
                    // Update if current direction is horizontal
                    return direction == RevealDirection.FromLeft || direction == RevealDirection.FromRight;
                    
                case RevealMode.Horizontal:
                    // Update if current direction is vertical
                    return direction == RevealDirection.FromTop || direction == RevealDirection.FromBottom;
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Gets the default direction for a given mode
        /// </summary>
        private RevealDirection GetDefaultDirectionForMode(RevealMode mode)
        {
            switch (mode)
            {
                case RevealMode.Vertical:
                    return RevealDirection.FromTop;
                    
                case RevealMode.Horizontal:
                    return RevealDirection.FromLeft;
                    
                default:
                    return _currentDirection; // Keep current for other modes
            }
        }
        
        private void RevealSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (RevealSlider == null) return; // ✨ Add null check
            
            _revealAmount = RevealSlider.Value;
            UpdateRevealLabel();
            ApplyReveal();
        }
        
        private void UpdateRevealLabel()
        {
            if (RevealLabel == null) return;
            
            switch (_currentMode)
            {
                case RevealMode.Vertical:
                case RevealMode.Horizontal:
                    int percentage = (int)(_revealAmount * 100);
                    RevealLabel.Text = $"Hiện: {percentage}%/100%";
                    break;
                    
                case RevealMode.Spotlight:
                    int size = (int)(_revealAmount * 100);
                    RevealLabel.Text = $"Kích thước: {size}%";
                    break;
            }
        }
        
        #endregion
        
        #region Apply Reveal
        
        private void ApplyReveal()
        {
            if (CurtainOverlay == null) return;
            
            switch (_currentMode)
            {
                case RevealMode.Vertical:
                    ApplyVerticalReveal();
                    break;
                    
                case RevealMode.Horizontal:
                    ApplyHorizontalReveal();
                    break;
                    
                case RevealMode.Spotlight:
                    ApplySpotlight();
                    break;
                    
                case RevealMode.CustomArea:
                    // Custom areas handled by mouse events
                    CurtainOverlay.Clip = null;
                    break;
            }
        }
        
        private void ApplyVerticalReveal()
        {
            double screenHeight = this.Height;
            double revealHeight = screenHeight * _revealAmount;
            
            RectangleGeometry clip;
            
            // ✨ PHASE 2: Support direction
            if (_currentDirection == RevealDirection.FromTop)
            {
                // Reveal from top
                clip = new RectangleGeometry(new Rect(0, 0, this.Width, revealHeight));
            }
            else // FromBottom
            {
                // Reveal from bottom
                clip = new RectangleGeometry(new Rect(0, screenHeight - revealHeight, this.Width, revealHeight));
            }
            
            // Invert: make revealed area transparent
            CombinedGeometry combined = new CombinedGeometry(
                GeometryCombineMode.Exclude,
                new RectangleGeometry(new Rect(0, 0, this.Width, this.Height)),
                clip
            );
            
            CurtainOverlay.Clip = combined;
        }
        
        private void ApplyHorizontalReveal()
        {
            double screenWidth = this.Width;
            double revealWidth = screenWidth * _revealAmount;
            
            RectangleGeometry clip;
            
            // ✨ PHASE 2: Support direction
            if (_currentDirection == RevealDirection.FromLeft)
            {
                // Reveal from left
                clip = new RectangleGeometry(new Rect(0, 0, revealWidth, this.Height));
            }
            else // FromRight
            {
                // Reveal from right
                clip = new RectangleGeometry(new Rect(screenWidth - revealWidth, 0, revealWidth, this.Height));
            }
            
            // Invert
            CombinedGeometry combined = new CombinedGeometry(
                GeometryCombineMode.Exclude,
                new RectangleGeometry(new Rect(0, 0, this.Width, this.Height)),
                clip
            );
            
            CurtainOverlay.Clip = combined;
        }
        
        private void ApplySpotlight()
        {
            double radius = Math.Min(this.Width, this.Height) * _revealAmount * 0.5;
            
            // ✨ PHASE 2: Use spotlight position (center or mouse)
            EllipseGeometry spotlight = new EllipseGeometry(
                _spotlightPosition,
                radius, radius
            );
            
            // Invert
            CombinedGeometry combined = new CombinedGeometry(
                GeometryCombineMode.Exclude,
                new RectangleGeometry(new Rect(0, 0, this.Width, this.Height)),
                spotlight
            );
            
            CurtainOverlay.Clip = combined;
        }
        
        #endregion
        
        #region Quick Actions
        
        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            OpacitySlider.Value = 0.9;
            RevealSlider.Value = 0.2;
            RevealModeCombo.SelectedIndex = 0;
        }
        
        private void ShowAll_Click(object sender, RoutedEventArgs e)
        {
            // Animate to fully revealed
            DoubleAnimation anim = new DoubleAnimation
            {
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            RevealSlider.BeginAnimation(Slider.ValueProperty, anim);
        }
        
        private void HideAll_Click(object sender, RoutedEventArgs e)
        {
            // Animate to fully hidden
            DoubleAnimation anim = new DoubleAnimation
            {
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            RevealSlider.BeginAnimation(Slider.ValueProperty, anim);
        }
        
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        
        #endregion
        
        #region Panel Dragging
        
        private void ControlPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _isDraggingPanel = true;
                _dragStartPoint = e.GetPosition(this);
                ControlPanel.CaptureMouse();
            }
        }
        
        private void ControlPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingPanel && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(this);
                double deltaX = currentPoint.X - _dragStartPoint.X;
                double deltaY = currentPoint.Y - _dragStartPoint.Y;
                
                // Update margin to move panel
                Thickness margin = ControlPanel.Margin;
                margin.Right -= deltaX;
                margin.Top += deltaY;
                
                // Clamp to screen bounds
                margin.Right = Math.Max(0, Math.Min(margin.Right, this.Width - ControlPanel.ActualWidth));
                margin.Top = Math.Max(0, Math.Min(margin.Top, this.Height - ControlPanel.ActualHeight));
                
                ControlPanel.Margin = margin;
                _dragStartPoint = currentPoint;
            }
        }
        
        private void ControlPanel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingPanel)
            {
                _isDraggingPanel = false;
                ControlPanel.ReleaseMouseCapture();
            }
        }
        
        #endregion
        
        #region Minimize/Maximize Panel
        
        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            ControlPanel.Visibility = Visibility.Collapsed;
            MinimizedPanel.Visibility = Visibility.Visible;
        }
        
        private void MinimizedPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MinimizedPanel.Visibility = Visibility.Collapsed;
            ControlPanel.Visibility = Visibility.Visible;
        }
        
        private void btnClosePanel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        
        #endregion
        
        #region Keyboard Shortcuts
        
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    this.Close();
                    break;
                    
                case Key.Space:
                    // Toggle show/hide all
                    if (RevealSlider.Value > 0.5)
                        HideAll_Click(null, null);
                    else
                        ShowAll_Click(null, null);
                    break;
                    
                case Key.Up:
                    RevealSlider.Value = Math.Min(1.0, RevealSlider.Value + 0.1);
                    break;
                    
                case Key.Down:
                    RevealSlider.Value = Math.Max(0.0, RevealSlider.Value - 0.1);
                    break;
                    
                case Key.R:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                        Reset_Click(null, null);
                    break;
            }
        }
        
        #endregion
        
        #region PHASE 2: Direction Control & Spotlight Movement
        
        private void UpdatePanelVisibility()
        {
            if (DirectionPanel == null || SpotlightPanel == null) return;
            
            switch (_currentMode)
            {
                case RevealMode.Vertical:
                case RevealMode.Horizontal:
                    DirectionPanel.Visibility = Visibility.Visible;
                    SpotlightPanel.Visibility = Visibility.Collapsed;
                    break;
                    
                case RevealMode.Spotlight:
                    DirectionPanel.Visibility = Visibility.Collapsed;
                    SpotlightPanel.Visibility = Visibility.Visible;
                    break;
                    
                default:
                    DirectionPanel.Visibility = Visibility.Collapsed;
                    SpotlightPanel.Visibility = Visibility.Collapsed;
                    break;
            }
        }
        
        private void Direction_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (DirectionCombo == null || DirectionCombo.SelectedIndex < 0) return;
            
            _currentDirection = (RevealDirection)DirectionCombo.SelectedIndex;
            
            // ✨ PHASE 2: Auto-switch mode based on direction
            // Prevent infinite loop by checking if mode needs to change
            RevealMode requiredMode = GetRequiredModeForDirection(_currentDirection);
            
            if (_currentMode != requiredMode && RevealModeCombo != null)
            {
                // Temporarily remove event handler to prevent recursive calls
                RevealModeCombo.SelectionChanged -= RevealMode_Changed;
                
                // Switch to appropriate mode
                RevealModeCombo.SelectedIndex = (int)requiredMode;
                _currentMode = requiredMode;
                
                // Re-attach event handler
                RevealModeCombo.SelectionChanged += RevealMode_Changed;
            }
            
            ApplyReveal();
        }
        
        /// <summary>
        /// Determines which reveal mode is required for a given direction
        /// </summary>
        private RevealMode GetRequiredModeForDirection(RevealDirection direction)
        {
            switch (direction)
            {
                case RevealDirection.FromTop:
                case RevealDirection.FromBottom:
                    return RevealMode.Vertical;
                    
                case RevealDirection.FromLeft:
                case RevealDirection.FromRight:
                    return RevealMode.Horizontal;
                    
                default:
                    return _currentMode; // Keep current mode for unknown directions
            }
        }
        
        private void FollowMouse_Changed(object sender, RoutedEventArgs e)
        {
            if (chkFollowMouse == null) return;
            
            _isFollowingMouse = chkFollowMouse.IsChecked == true;
            
            if (!_isFollowingMouse)
            {
                _spotlightPosition = new Point(this.Width / 2, this.Height / 2);
                if (txtSpotlightPos != null)
                {
                    txtSpotlightPos.Text = "Position: Center";
                }
                ApplyReveal();
            }
        }
        
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isFollowingMouse && _currentMode == RevealMode.Spotlight)
            {
                _spotlightPosition = e.GetPosition(this);
                
                if (txtSpotlightPos != null)
                {
                    txtSpotlightPos.Text = $"Pos: ({(int)_spotlightPosition.X}, {(int)_spotlightPosition.Y})";
                }
                
                ApplyReveal();
            }
        }
        
        #endregion
    }
}
