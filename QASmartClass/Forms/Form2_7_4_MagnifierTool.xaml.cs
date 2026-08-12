using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace QASmartTouch.Forms
{
    public partial class Form2_7_4_MagnifierTool : Window
    {
        // Common
        private double _spotlightSize = 200; // Match XAML default
        private Point _lastMousePosition;
        private Point _lastRenderedPosition; // For throttling
        private DispatcherTimer? _updateTimer;
        
        // Magnify mode
        private double _currentZoom = 2.0;
        private bool _isMagnifyMode = false; // Default to Spotlight mode
        private bool _startInMagnifyMode = false;
        
        // Spotlight mode
        private double _overlayOpacity = 0.8;
        private bool _isCircleShape = true;
        private SolidColorBrush? _spotlightBrush; // Cache brush
        private const double MOUSE_MOVE_THRESHOLD = 0.5; // Minimum pixels to trigger update

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        public Form2_7_4_MagnifierTool(bool startInMagnifyMode = false)
        {
            InitializeComponent();
            _startInMagnifyMode = startInMagnifyMode;
            
            // Setup update timer - 30 FPS for better performance
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33) // ~30 FPS (was 16ms/60 FPS)
            };
            _updateTimer.Tick += UpdateTimer_Tick;
            
            // Initialize spotlight brush
            byte alpha = (byte)(_overlayOpacity * 255);
            _spotlightBrush = new SolidColorBrush(Color.FromArgb(alpha, 0, 0, 0));

            // Track touch/stylus movements
            this.PreviewTouchMove += (s, e) =>
            {
                try
                {
                    TouchPoint tp = e.GetTouchPoint(this);
                    _lastMousePosition = tp.Position;
                }
                catch { }
            };
            this.PreviewStylusMove += (s, e) =>
            {
                try
                {
                    _lastMousePosition = e.GetPosition(this);
                }
                catch { }
            };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set canvas size to full screen
            SpotlightCanvas.Width = this.ActualWidth;
            SpotlightCanvas.Height = this.ActualHeight;
            CrosshairCanvas.Width = this.ActualWidth;
            CrosshairCanvas.Height = this.ActualHeight;
            
            // Show spotlight mode or magnify mode based on parameter
            if (_startInMagnifyMode)
            {
                SwitchToMagnifyMode();
            }
            else
            {
                SwitchToSpotlightMode();
            }
            
            // Show control panel
            ControlPanel.Visibility = Visibility.Visible;
            
            // Start update timer
            _updateTimer?.Start();
        }

        private bool IsPositionInsideControlPanel(Point pos)
        {
            if (ControlPanel == null || ControlPanel.Visibility != Visibility.Visible)
                return false;

            try
            {
                Point relativePos = ControlPanel.TranslatePoint(new Point(0, 0), this);
                Rect panelRect = new Rect(relativePos.X, relativePos.Y, ControlPanel.ActualWidth, ControlPanel.ActualHeight);
                panelRect.Inflate(10, 10);
                return panelRect.Contains(pos);
            }
            catch
            {
                return false;
            }
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            // Real-time mouse/touch/stylus tracking via Win32 GetCursorPos
            if (this.IsLoaded && GetCursorPos(out POINT screenPt))
            {
                try
                {
                    Point wndPt = this.PointFromScreen(new Point(screenPt.X, screenPt.Y));
                    _lastMousePosition = wndPt;
                }
                catch
                {
                    // Ignore transient conversion exceptions during window lifecycle
                }
            }

            if (_isMagnifyMode)
            {
                CrosshairCanvas.Visibility = Visibility.Visible;
                UpdateMagnifier();
            }
            else
            {
                UpdateSpotlight();
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            _lastMousePosition = e.GetPosition(this);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Future: Fixed position mode
        }

        #region Mode Switching

        private void btnMagnifyMode_Click(object sender, RoutedEventArgs e)
        {
            SwitchToMagnifyMode();
        }

        private void btnSpotlightMode_Click(object sender, RoutedEventArgs e)
        {
            SwitchToSpotlightMode();
        }

        private void SwitchToMagnifyMode()
        {
            _isMagnifyMode = true;
            
            // Update UI
            MagnifierBorder.Visibility = Visibility.Visible;
            SpotlightCanvas.Visibility = Visibility.Collapsed;
            CrosshairCanvas.Visibility = Visibility.Visible;
            
            MagnifyControls.Visibility = Visibility.Visible;
            SpotlightControls.Visibility = Visibility.Collapsed;
            
            // Update buttons
            btnMagnifyMode.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
            btnMagnifyMode.Foreground = Brushes.White;
            btnSpotlightMode.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F2F6"));
            btnSpotlightMode.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F3542"));
            
            txtTitle.Text = "🔍 Đèn soi - Phóng to";
            txtSizeLabel.Text = "Kích thước:";
            
            // Update size slider for magnifier
            sizeSlider.Maximum = 400;
            sizeSlider.Value = Math.Min(_spotlightSize, 400);
        }

        private void SwitchToSpotlightMode()
        {
            _isMagnifyMode = false;
            
            // Update UI
            MagnifierBorder.Visibility = Visibility.Collapsed;
            SpotlightCanvas.Visibility = Visibility.Visible;
            CrosshairCanvas.Visibility = Visibility.Collapsed;
            
            MagnifyControls.Visibility = Visibility.Collapsed;
            SpotlightControls.Visibility = Visibility.Visible;
            
            // Update buttons
            btnMagnifyMode.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F2F6"));
            btnMagnifyMode.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F3542"));
            btnSpotlightMode.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
            btnSpotlightMode.Foreground = Brushes.White;
            
            txtTitle.Text = "🎭 Đèn soi - Che màn";
            txtSizeLabel.Text = "Kích thước vùng sáng:";
            
            // Update size slider for spotlight
            sizeSlider.Maximum = 800;
            sizeSlider.Value = Math.Max(_spotlightSize, 300);
            
            // Ensure canvas is sized correctly
            SpotlightCanvas.Width = this.ActualWidth;
            SpotlightCanvas.Height = this.ActualHeight;
            
            // Initial spotlight update
            UpdateSpotlight();
        }

        #endregion

        #region Magnify Mode

        private void UpdateMagnifier()
        {
            try
            {
                // Ensure magnifier size is set
                if (MagnifierBorder.Width != _spotlightSize)
                {
                    MagnifierBorder.Width = _spotlightSize;
                    MagnifierBorder.Height = _spotlightSize;
                    MagnifierBorder.CornerRadius = new CornerRadius(_spotlightSize / 2);
                    
                    double radius = _spotlightSize / 2 - 4;
                    MagnifierClip.RadiusX = radius;
                    MagnifierClip.RadiusY = radius;
                    MagnifierClip.Center = new Point(_spotlightSize / 2, _spotlightSize / 2);
                }
                
                // Position magnifier at mouse
                double offsetX = _spotlightSize / 2;
                double offsetY = _spotlightSize / 2;
                
                Canvas.SetLeft(MagnifierBorder, _lastMousePosition.X - offsetX);
                Canvas.SetTop(MagnifierBorder, _lastMousePosition.Y - offsetY);

                // Update crosshair
                UpdateCrosshair();

                // Capture and magnify
                CaptureAndMagnify();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Magnifier error: {ex.Message}");
            }
        }

        private void UpdateCrosshair()
        {
            CrosshairH.X1 = 0;
            CrosshairH.Y1 = _lastMousePosition.Y;
            CrosshairH.X2 = this.ActualWidth;
            CrosshairH.Y2 = _lastMousePosition.Y;

            CrosshairV.X1 = _lastMousePosition.X;
            CrosshairV.Y1 = 0;
            CrosshairV.X2 = _lastMousePosition.X;
            CrosshairV.Y2 = this.ActualHeight;
        }

        private void CaptureAndMagnify()
        {
            try
            {
                int captureSize = (int)(_spotlightSize / _currentZoom);
                int captureX = (int)(_lastMousePosition.X - captureSize / 2);
                int captureY = (int)(_lastMousePosition.Y - captureSize / 2);

                // Convert window local point to screen coordinates (Fixes double.NaN issue on Maximized WindowState)
                Point screenTopLeft = this.PointToScreen(new Point(captureX, captureY));
                int screenX = (int)screenTopLeft.X;
                int screenY = (int)screenTopLeft.Y;

                int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
                int screenHeight = (int)SystemParameters.PrimaryScreenHeight;

                screenX = Math.Max(0, Math.Min(screenX, screenWidth - captureSize));
                screenY = Math.Max(0, Math.Min(screenY, screenHeight - captureSize));

                var bitmap = new System.Drawing.Bitmap(captureSize, captureSize);
                using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(
                        screenX,
                        screenY,
                        0, 0,
                        new System.Drawing.Size(captureSize, captureSize));
                }

                var handle = bitmap.GetHbitmap();
                try
                {
                    var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        handle,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());

                    MagnifiedImage.Source = bitmapSource;
                }
                finally
                {
                    DeleteObject(handle);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Capture error: {ex.Message}");
            }
        }

        private void btnZoom_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string zoomStr)
            {
                if (double.TryParse(zoomStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double zoom))
                {
                    _currentZoom = zoom;
                    MagnifyScale.ScaleX = _currentZoom;
                    MagnifyScale.ScaleY = _currentZoom;
                    
                    UpdateZoomButtons();
                }
            }
        }

        private void UpdateZoomButtons()
        {
            btn2x.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F2F6"));
            btn2x.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F3542"));
            btn3x.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F2F6"));
            btn3x.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F3542"));
            btn5x.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F2F6"));
            btn5x.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F3542"));

            Button? activeButton = null;
            if (Math.Abs(_currentZoom - 2.0) < 0.1) activeButton = btn2x;
            else if (Math.Abs(_currentZoom - 3.0) < 0.1) activeButton = btn3x;
            else if (Math.Abs(_currentZoom - 5.0) < 0.1) activeButton = btn5x;

            if (activeButton != null)
            {
                activeButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
                activeButton.Foreground = Brushes.White;
            }
        }

        #endregion

        #region Spotlight Mode

        private void UpdateSpotlight()
        {
            try
            {
                // Throttle updates - only update if mouse moved enough
                double distance = Math.Sqrt(
                    Math.Pow(_lastMousePosition.X - _lastRenderedPosition.X, 2) +
                    Math.Pow(_lastMousePosition.Y - _lastRenderedPosition.Y, 2));
                
                if (distance < MOUSE_MOVE_THRESHOLD)
                    return; // Skip update if mouse didn't move much
                
                _lastRenderedPosition = _lastMousePosition;
                
                // Create geometry with hole
                var geometryGroup = new GeometryGroup();
                geometryGroup.FillRule = FillRule.EvenOdd;

                // Full screen rectangle
                geometryGroup.Children.Add(new RectangleGeometry(
                    new Rect(0, 0, this.ActualWidth, this.ActualHeight)));

                // Spotlight hole (subtract from full screen)
                if (_isCircleShape)
                {
                    double radius = _spotlightSize / 2;
                    geometryGroup.Children.Add(new EllipseGeometry(
                        _lastMousePosition, radius, radius));
                }
                else
                {
                    double halfSize = _spotlightSize / 2;
                    geometryGroup.Children.Add(new RectangleGeometry(
                        new Rect(
                            _lastMousePosition.X - halfSize,
                            _lastMousePosition.Y - halfSize,
                            _spotlightSize,
                            _spotlightSize)));
                }

                // Apply geometry to path
                SpotlightPath.Data = geometryGroup;
                
                // Use cached brush (only recreate if opacity changed)
                if (_spotlightBrush != null)
                {
                    SpotlightPath.Fill = _spotlightBrush;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Spotlight error: {ex.Message}");
            }
        }

        private void opacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtOpacity == null) return;
            
            _overlayOpacity = e.NewValue / 100.0;
            txtOpacity.Text = $"{(int)e.NewValue}%";
            
            // Recreate cached brush with new opacity
            byte alpha = (byte)(_overlayOpacity * 255);
            _spotlightBrush = new SolidColorBrush(Color.FromArgb(alpha, 0, 0, 0));
            
            if (!_isMagnifyMode)
            {
                // Force immediate update
                _lastRenderedPosition = new Point(-1000, -1000); // Reset throttle
                UpdateSpotlight();
            }
        }

        private void btnCircleShape_Click(object sender, RoutedEventArgs e)
        {
            _isCircleShape = true;
            
            btnCircleShape.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
            btnCircleShape.Foreground = Brushes.White;
            btnSquareShape.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F2F6"));
            btnSquareShape.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F3542"));
            
            // Force immediate update
            _lastRenderedPosition = new Point(-1000, -1000);
            UpdateSpotlight();
        }

        private void btnSquareShape_Click(object sender, RoutedEventArgs e)
        {
            _isCircleShape = false;
            
            btnSquareShape.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
            btnSquareShape.Foreground = Brushes.White;
            btnCircleShape.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F2F6"));
            btnCircleShape.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F3542"));
            
            // Force immediate update
            _lastRenderedPosition = new Point(-1000, -1000);
            UpdateSpotlight();
        }

        #endregion

        #region Common Controls

        private void sizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MagnifierBorder == null || MagnifierClip == null || txtSize == null)
                return;
                
            _spotlightSize = e.NewValue;
            
            if (_isMagnifyMode)
            {
                // Update magnifier size
                MagnifierBorder.Width = _spotlightSize;
                MagnifierBorder.Height = _spotlightSize;
                
                double radius = _spotlightSize / 2 - 4;
                MagnifierClip.RadiusX = radius;
                MagnifierClip.RadiusY = radius;
                MagnifierClip.Center = new Point(_spotlightSize / 2, _spotlightSize / 2);
            }
            else
            {
                // Update spotlight hole size
                UpdateSpotlight();
            }
            
            txtSize.Text = $"{(int)_spotlightSize} px";
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_isMagnifyMode)
            {
                // Zoom in/out
                if (e.Delta > 0)
                {
                    if (_currentZoom < 5.0)
                    {
                        _currentZoom = Math.Min(5.0, _currentZoom + 0.5);
                    }
                }
                else
                {
                    if (_currentZoom > 1.5)
                    {
                        _currentZoom = Math.Max(1.5, _currentZoom - 0.5);
                    }
                }

                MagnifyScale.ScaleX = _currentZoom;
                MagnifyScale.ScaleY = _currentZoom;
                
                UpdateZoomButtons();
            }
            else
            {
                // Adjust spotlight size
                if (e.Delta > 0)
                {
                    sizeSlider.Value = Math.Min(600, sizeSlider.Value + 50);
                }
                else
                {
                    sizeSlider.Value = Math.Max(100, sizeSlider.Value - 50);
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                btnClose_Click(null, null);
            }
            else if (e.Key == Key.M)
            {
                SwitchToMagnifyMode();
            }
            else if (e.Key == Key.S)
            {
                SwitchToSpotlightMode();
            }
            else if (e.Key == Key.C && !_isMagnifyMode)
            {
                btnCircleShape_Click(null, null);
            }
            else if (e.Key == Key.Q && !_isMagnifyMode)
            {
                btnSquareShape_Click(null, null);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            _updateTimer?.Stop();
            this.Close();
        }

        #endregion

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
    }
}
