using System;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using QASmartTouch.Helpers;

namespace QASmartTouch.Forms
{
    public partial class Form2_19_CountdownTimer : Window
    {
        private DispatcherTimer _timer;
        private TimeSpan _totalTime;
        private TimeSpan _remainingTime;
        private TimerState _state = TimerState.Stopped;
        
        public enum TimerState
        {
            Stopped,   // Initial state, can edit time
            Running,   // Counting down
            Paused,    // Paused, can resume
            Completed  // Time's up
        }
        
        public Form2_19_CountdownTimer()
        {
            InitializeComponent();
            TouchActivationHelper.Apply(this);
            
            // Initialize timer
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            
            // Set initial time
            _totalTime = TimeSpan.FromMinutes(10);
            _remainingTime = _totalTime;

            // Register pasting and lost focus handlers
            DataObject.AddPastingHandler(txtHours, TextBox_Pasting);
            DataObject.AddPastingHandler(txtMinutes, TextBox_Pasting);
            DataObject.AddPastingHandler(txtSeconds, TextBox_Pasting);

            txtHours.LostFocus += TextBox_LostFocus;
            txtMinutes.LostFocus += TextBox_LostFocus;
            txtSeconds.LostFocus += TextBox_LostFocus;
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Position window at bottom-right
            this.Left = SystemParameters.WorkArea.Right - this.Width - 20;
            this.Top = SystemParameters.WorkArea.Bottom - this.Height - 20;
            
            UpdateDisplay();
            UpdateProgressRing();
        }
        
        #region Timer Logic
        
        private void Timer_Tick(object sender, EventArgs e)
        {
            _remainingTime = _remainingTime.Subtract(TimeSpan.FromSeconds(1));
            
            UpdateDisplay();
            UpdateProgressRing();
            
            if (_remainingTime.TotalSeconds <= 0)
            {
                TimerCompleted();
            }
        }
        
        private void TimerCompleted()
        {
            _timer.Stop();
            _state = TimerState.Completed;
            _remainingTime = TimeSpan.Zero;
            
            UpdateDisplay();
            UpdateProgressRing();
            UpdateButtonStates();
            UpdateStatus("⏰ Hết giờ!", Colors.Red);
            
            // Play sound
            SystemSounds.Beep.Play();
            
            // Flash window
            FlashWindow();
            
            // Removed MessageBox notification - user can see status and hear sound
        }
        
        private void FlashWindow()
        {
            // Flash border color
            var border = (Border)this.Content;
            var originalBrush = border.BorderBrush;
            
            ColorAnimation flashAnimation = new ColorAnimation
            {
                From = Colors.White,
                To = Colors.Red,
                Duration = TimeSpan.FromMilliseconds(300),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(5)
            };
            
            SolidColorBrush brush = new SolidColorBrush(Colors.White);
            border.BorderBrush = brush;
            border.BorderThickness = new Thickness(3);
            
            brush.BeginAnimation(SolidColorBrush.ColorProperty, flashAnimation);
            
            // Reset after animation
            DispatcherTimer resetTimer = new DispatcherTimer();
            resetTimer.Interval = TimeSpan.FromSeconds(2);
            resetTimer.Tick += (s, e) =>
            {
                border.BorderBrush = originalBrush;
                border.BorderThickness = new Thickness(0);
                resetTimer.Stop();
            };
            resetTimer.Start();
        }
        
        #endregion
        
        #region Display Updates
        
        private void UpdateDisplay()
        {
            int totalSeconds = (int)_remainingTime.TotalSeconds;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            
            txtTimerDisplay.Text = $"{minutes:D2}:{seconds:D2}";
        }
        
        private void UpdateProgressRing()
        {
            if (_totalTime.TotalSeconds == 0) return;
            
            double percentage = _remainingTime.TotalSeconds / _totalTime.TotalSeconds;
            percentage = Math.Max(0, Math.Min(1, percentage)); // Clamp 0-1
            
            // Create arc path
            double radius = 120; // Half of 240px diameter (minus stroke)
            double centerX = 130;
            double centerY = 130;
            
            // Calculate arc angle (360 degrees * percentage)
            double angle = 360 * percentage;
            if (angle >= 360) angle = 359.99;
            
            // Convert to radians
            double startAngle = -90; // Start from top
            double endAngle = startAngle + angle;
            
            double startRad = startAngle * Math.PI / 180;
            double endRad = endAngle * Math.PI / 180;
            
            // Calculate arc points
            Point startPoint = new Point(
                centerX + radius * Math.Cos(startRad),
                centerY + radius * Math.Sin(startRad)
            );
            
            Point endPoint = new Point(
                centerX + radius * Math.Cos(endRad),
                centerY + radius * Math.Sin(endRad)
            );
            
            // Create path
            bool isLargeArc = angle > 180;
            
            PathGeometry pathGeometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure();
            pathFigure.StartPoint = startPoint;
            
            ArcSegment arcSegment = new ArcSegment();
            arcSegment.Point = endPoint;
            arcSegment.Size = new Size(radius, radius);
            arcSegment.IsLargeArc = isLargeArc;
            arcSegment.SweepDirection = SweepDirection.Clockwise;
            
            pathFigure.Segments.Add(arcSegment);
            pathGeometry.Figures.Add(pathFigure);
            
            ProgressArc.Data = pathGeometry;
            
            // Change color based on time remaining
            if (percentage < 0.1)
            {
                ProgressArc.Stroke = new SolidColorBrush(Color.FromRgb(229, 62, 62)); // Red
            }
            else if (percentage < 0.25)
            {
                ProgressArc.Stroke = new SolidColorBrush(Color.FromRgb(237, 137, 54)); // Orange
            }
            else
            {
                ProgressArc.Stroke = new SolidColorBrush(Color.FromRgb(49, 130, 206)); // Blue #3182ce
            }
        }
        
        private void UpdateStatus(string text, Color color)
        {
            txtStatus.Text = text;
            statusDot.Fill = new SolidColorBrush(color);
        }
        
        private void UpdateButtonStates()
        {
            switch (_state)
            {
                case TimerState.Stopped:
                    btnStart.Content = "▶ Bắt đầu";
                    btnStart.IsEnabled = true;
                    btnPause.IsEnabled = false;
                    btnReset.IsEnabled = true;
                    txtHours.IsEnabled = true;
                    txtMinutes.IsEnabled = true;
                    txtSeconds.IsEnabled = true;
                    UpdateStatus("Sẵn sàng", Color.FromRgb(203, 213, 224));
                    break;
                    
                case TimerState.Running:
                    btnStart.Content = "▶ Tiếp tục";
                    btnStart.IsEnabled = false;
                    btnPause.IsEnabled = true;
                    btnReset.IsEnabled = true;
                    txtHours.IsEnabled = false;
                    txtMinutes.IsEnabled = false;
                    txtSeconds.IsEnabled = false;
                    UpdateStatus("Đang chạy", Color.FromRgb(72, 187, 120));
                    break;
                    
                case TimerState.Paused:
                    btnStart.Content = "▶ Tiếp tục";
                    btnStart.IsEnabled = true;
                    btnPause.IsEnabled = false;
                    btnReset.IsEnabled = true;
                    txtHours.IsEnabled = false;
                    txtMinutes.IsEnabled = false;
                    txtSeconds.IsEnabled = false;
                    UpdateStatus("Tạm dừng", Color.FromRgb(237, 137, 54));
                    break;
                    
                case TimerState.Completed:
                    btnStart.Content = "▶ Bắt đầu";
                    btnStart.IsEnabled = false;
                    btnPause.IsEnabled = false;
                    btnReset.IsEnabled = true;
                    txtHours.IsEnabled = true;
                    txtMinutes.IsEnabled = true;
                    txtSeconds.IsEnabled = true;
                    break;
            }
        }
        
        #endregion
        
        #region Button Handlers
        
        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (_state == TimerState.Stopped)
            {
                // Get time from inputs
                int hours = int.TryParse(txtHours.Text, out int h) ? h : 0;
                int minutes = int.TryParse(txtMinutes.Text, out int m) ? m : 0;
                int seconds = int.TryParse(txtSeconds.Text, out int s) ? s : 0;
                
                _totalTime = new TimeSpan(hours, minutes, seconds);
                _remainingTime = _totalTime;
                
                if (_totalTime.TotalSeconds == 0)
                {
                    MessageBox.Show("Vui lòng nhập thời gian lớn hơn 0!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            
            _state = TimerState.Running;
            _timer.Start();
            UpdateButtonStates();
        }
        
        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            _state = TimerState.Paused;
            _timer.Stop();
            UpdateButtonStates();
        }
        
        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _state = TimerState.Stopped;
            
            // Reset to input time
            int hours = int.TryParse(txtHours.Text, out int h) ? h : 0;
            int minutes = int.TryParse(txtMinutes.Text, out int m) ? m : 0;
            int seconds = int.TryParse(txtSeconds.Text, out int s) ? s : 0;
            
            _totalTime = new TimeSpan(hours, minutes, seconds);
            _remainingTime = _totalTime;
            
            UpdateDisplay();
            UpdateProgressRing();
            UpdateButtonStates();
        }
        
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
        
        #endregion
        
        #region Preset Buttons
        
        private void Preset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                int minutes = int.Parse(tag);
                
                // Update input fields
                txtHours.Text = (minutes / 60).ToString("D2");
                txtMinutes.Text = (minutes % 60).ToString("D2");
                txtSeconds.Text = "00";
                
                // Update timer display immediately
                _totalTime = TimeSpan.FromMinutes(minutes);
                _remainingTime = _totalTime;
                
                UpdateDisplay();
                UpdateProgressRing();
                
                // Auto-reset if timer was running
                if (_state != TimerState.Stopped)
                {
                    btnReset_Click(null, null);
                }
            }
        }
        
        #endregion
        
        #region Increment/Decrement Buttons
        
        private void btnHoursUp_Click(object sender, RoutedEventArgs e)
        {
            int currentValue = int.TryParse(txtHours.Text, out int val) ? val : 0;
            currentValue = (currentValue + 1) % 24; // Wrap around at 24
            txtHours.Text = currentValue.ToString("D2");
            UpdateTimerFromInputs();
        }
        
        private void btnHoursDown_Click(object sender, RoutedEventArgs e)
        {
            int currentValue = int.TryParse(txtHours.Text, out int val) ? val : 0;
            currentValue = (currentValue - 1 + 24) % 24; // Wrap around at 0
            txtHours.Text = currentValue.ToString("D2");
            UpdateTimerFromInputs();
        }
        
        private void btnMinutesUp_Click(object sender, RoutedEventArgs e)
        {
            int currentValue = int.TryParse(txtMinutes.Text, out int val) ? val : 0;
            currentValue = (currentValue + 1) % 60; // Wrap around at 60
            txtMinutes.Text = currentValue.ToString("D2");
            UpdateTimerFromInputs();
        }
        
        private void btnMinutesDown_Click(object sender, RoutedEventArgs e)
        {
            int currentValue = int.TryParse(txtMinutes.Text, out int val) ? val : 0;
            currentValue = (currentValue - 1 + 60) % 60; // Wrap around at 0
            txtMinutes.Text = currentValue.ToString("D2");
            UpdateTimerFromInputs();
        }
        
        private void btnSecondsUp_Click(object sender, RoutedEventArgs e)
        {
            int currentValue = int.TryParse(txtSeconds.Text, out int val) ? val : 0;
            currentValue = (currentValue + 1) % 60; // Wrap around at 60
            txtSeconds.Text = currentValue.ToString("D2");
            UpdateTimerFromInputs();
        }
        
        private void btnSecondsDown_Click(object sender, RoutedEventArgs e)
        {
            int currentValue = int.TryParse(txtSeconds.Text, out int val) ? val : 0;
            currentValue = (currentValue - 1 + 60) % 60; // Wrap around at 0
            txtSeconds.Text = currentValue.ToString("D2");
            UpdateTimerFromInputs();
        }
        
        private void UpdateTimerFromInputs()
        {
            // Only update if timer is stopped
            if (_state == TimerState.Stopped)
            {
                int hours = int.TryParse(txtHours.Text, out int h) ? h : 0;
                int minutes = int.TryParse(txtMinutes.Text, out int m) ? m : 0;
                int seconds = int.TryParse(txtSeconds.Text, out int s) ? s : 0;
                
                _totalTime = new TimeSpan(hours, minutes, seconds);
                _remainingTime = _totalTime;
                
                UpdateDisplay();
                UpdateProgressRing();
            }
        }
        
        #endregion
        
        #region Input Validation
        
        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Only allow digits
            e.Handled = !int.TryParse(e.Text, out _);
        }
        
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTimerFromInputs();
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = "00";
                }
                else if (int.TryParse(textBox.Text, out int val))
                {
                    textBox.Text = val.ToString("D2");
                }
            }
            NormalizeInputTime();
        }

        private void NormalizeInputTime()
        {
            if (int.TryParse(txtHours.Text, out int hrs) &&
                int.TryParse(txtMinutes.Text, out int mins) &&
                int.TryParse(txtSeconds.Text, out int secs))
            {
                if (secs >= 60)
                {
                    mins += secs / 60;
                    secs = secs % 60;
                }
                if (mins >= 60)
                {
                    hrs += mins / 60;
                    mins = mins % 60;
                }
                if (hrs > 99)
                {
                    hrs = 99;
                }

                txtHours.Text = hrs.ToString("D2");
                txtMinutes.Text = mins.ToString("D2");
                txtSeconds.Text = secs.ToString("D2");

                UpdateTimerFromInputs();
            }
        }

        private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                string text = (string)e.DataObject.GetData(DataFormats.Text);
                if (!int.TryParse(text, out int val) || val < 0)
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
        
        #endregion
        
        #region Keyboard Shortcuts
        
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Space:
                    if (_state == TimerState.Running)
                        btnPause_Click(null, null);
                    else if (_state == TimerState.Stopped || _state == TimerState.Paused)
                        btnStart_Click(null, null);
                    e.Handled = true;
                    break;
                    
                case Key.R:
                    btnReset_Click(null, null);
                    e.Handled = true;
                    break;
                    
                case Key.Escape:
                    this.Close();
                    e.Handled = true;
                    break;
            }
        }
        
        #endregion
    }
}
