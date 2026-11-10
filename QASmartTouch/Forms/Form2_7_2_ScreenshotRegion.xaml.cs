using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace QASmartTouch.Forms
{
    public partial class Form2_7_2_ScreenshotRegion : Window
    {
        private Point _startPoint;
        private bool _isSelecting = false;
        private Rect _selectedRect;
        
        // ✨ NEW: Public property to store captured image
        public BitmapSource? CapturedImage { get; private set; }
        public bool PlaceOnBoard { get; private set; } = false;

        public Form2_7_2_ScreenshotRegion()
        {
            InitializeComponent();
            
            // ✨ FIX: Set window to cover entire screen (including taskbar)
            this.Left = 0;
            this.Top = 0;
            this.Width = SystemParameters.VirtualScreenWidth;
            this.Height = SystemParameters.VirtualScreenHeight;
            this.WindowState = WindowState.Normal; // Don't use Maximized
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _startPoint = e.GetPosition(this);
                _isSelecting = true;
                
                SelectionRect.Visibility = Visibility.Visible;
                InstructionPanel.Visibility = Visibility.Collapsed;
                SizeLabel.Visibility = Visibility.Visible;
                
                Canvas.SetLeft(SelectionRect, _startPoint.X);
                Canvas.SetTop(SelectionRect, _startPoint.Y);
                SelectionRect.Width = 0;
                SelectionRect.Height = 0;
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            Point currentPoint = e.GetPosition(this);
            
            // ✨ NEW: Update crosshair position
            CrosshairH.Y1 = CrosshairH.Y2 = currentPoint.Y;
            CrosshairV.X1 = CrosshairV.X2 = currentPoint.X;
            
            if (_isSelecting)
            {
                double x = Math.Min(_startPoint.X, currentPoint.X);
                double y = Math.Min(_startPoint.Y, currentPoint.Y);
                double width = Math.Abs(currentPoint.X - _startPoint.X);
                double height = Math.Abs(currentPoint.Y - _startPoint.Y);
                
                Canvas.SetLeft(SelectionRect, x);
                Canvas.SetTop(SelectionRect, y);
                SelectionRect.Width = width;
                SelectionRect.Height = height;
                
                // Update size label
                txtSize.Text = $"{(int)width} × {(int)height}";
                Canvas.SetLeft(SizeLabel, x + width + 10);
                Canvas.SetTop(SizeLabel, y);
                
                _selectedRect = new Rect(x, y, width, height);
                
                // ✨ NEW: Update overlay to create spotlight effect
                UpdateOverlay(x, y, width, height);
            }
        }
        
        /// <summary>
        /// Update 4 overlay rectangles to create spotlight effect on selected region
        /// </summary>
        private void UpdateOverlay(double x, double y, double width, double height)
        {
            // Top rectangle: from 0 to y
            OverlayTop.VerticalAlignment = VerticalAlignment.Top;
            OverlayTop.Height = y;
            
            // Bottom rectangle: from (y + height) to end
            OverlayBottom.Height = Math.Max(0, this.ActualHeight - (y + height));
            
            // Left rectangle: from 0 to x (only between top and bottom)
            OverlayLeft.Width = x;
            OverlayLeft.Margin = new Thickness(0, y, 0, this.ActualHeight - (y + height));
            
            // Right rectangle: from (x + width) to end (only between top and bottom)
            OverlayRight.Width = Math.Max(0, this.ActualWidth - (x + width));
            OverlayRight.Margin = new Thickness(0, y, 0, this.ActualHeight - (y + height));
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelecting && _selectedRect.Width > 10 && _selectedRect.Height > 10)
            {
                _isSelecting = false;
                
                try
                {
                    // Measure ActionPanel size to position it correctly
                    ActionPanel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    double panelWidth = ActionPanel.DesiredSize.Width;
                    double panelHeight = ActionPanel.DesiredSize.Height;
                    
                    // Place ActionPanel at the bottom-right of the selection rectangle
                    double panelLeft = _selectedRect.X + _selectedRect.Width - panelWidth;
                    double panelTop = _selectedRect.Y + _selectedRect.Height + 10;
                    
                    // Clamp to make sure it is fully visible on screen
                    if (panelLeft < 10) panelLeft = 10;
                    if (panelLeft + panelWidth > this.ActualWidth - 10) panelLeft = this.ActualWidth - panelWidth - 10;
                    
                    if (panelTop + panelHeight > this.ActualHeight - 10)
                    {
                        // Place above selection rectangle if it goes below screen
                        panelTop = _selectedRect.Y - panelHeight - 10;
                    }
                    if (panelTop < 10) panelTop = 10;
                    
                    // Set Margin to position ActionPanel dynamically in the Grid
                    ActionPanel.Margin = new Thickness(panelLeft, panelTop, 0, 0);
                    ActionPanel.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error positioning ActionPanel: {ex.Message}");
                    // Fallback to simple show
                    ActionPanel.Visibility = Visibility.Visible;
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                btnCancel_Click(null, null);
            }
            else if (e.Key == Key.Enter && ActionPanel.Visibility == Visibility.Visible)
            {
                btnSave_Click(null, null);
            }
            else if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                btnCopy_Click(null, null);
            }
            else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                btnSave_Click(null, null);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var bitmap = CaptureRegion();
                
                var saveDialog = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png|JPEG Image|*.jpg|All Files|*.*",
                    DefaultExt = "png",
                    FileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}"
                };
                
                if (saveDialog.ShowDialog() == true)
                {
                    SaveBitmap(bitmap, saveDialog.FileName);
                    MessageBox.Show($"Đã lưu ảnh:\n{saveDialog.FileName}", 
                                  "Thành công", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                    this.Close();
                }
                else
                {
                    // Restore window and UI elements if save dialog was cancelled
                    this.Visibility = Visibility.Visible;
                    SelectionRect.Visibility = Visibility.Visible;
                    SizeLabel.Visibility = Visibility.Visible;
                    CrosshairH.Visibility = Visibility.Visible;
                    CrosshairV.Visibility = Visibility.Visible;
                    OverlayTop.Visibility = Visibility.Visible;
                    OverlayBottom.Visibility = Visibility.Visible;
                    OverlayLeft.Visibility = Visibility.Visible;
                    OverlayRight.Visibility = Visibility.Visible;
                    ActionPanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu ảnh:\n{ex.Message}", 
                              "Lỗi", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
                // Restore window and UI elements on error
                this.Visibility = Visibility.Visible;
                SelectionRect.Visibility = Visibility.Visible;
                SizeLabel.Visibility = Visibility.Visible;
                CrosshairH.Visibility = Visibility.Visible;
                CrosshairV.Visibility = Visibility.Visible;
                OverlayTop.Visibility = Visibility.Visible;
                OverlayBottom.Visibility = Visibility.Visible;
                OverlayLeft.Visibility = Visibility.Visible;
                OverlayRight.Visibility = Visibility.Visible;
                ActionPanel.Visibility = Visibility.Visible;
            }
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var bitmap = CaptureRegion();
                Clipboard.SetImage(bitmap);
                
                MessageBox.Show("Đã copy ảnh vào clipboard!", 
                              "Thành công", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi copy ảnh:\n{ex.Message}", 
                              "Lỗi", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        
        private void btnPlaceOnBoard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Capture the selected region
                CapturedImage = CaptureRegion();
                PlaceOnBoard = true;
                
                // Close dialog with success
                // ✅ Only set DialogResult if window is shown as dialog
                try
                {
                    if (this.IsLoaded)
                    {
                        this.DialogResult = true;
                    }
                }
                catch
                {
                    // If DialogResult cannot be set, just close
                }
                
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi chụp ảnh:\n{ex.Message}", 
                              "Lỗi", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        private BitmapSource CaptureRegion()
        {
            // Get DPI scale of current screen
            var dpi = VisualTreeHelper.GetDpi(this);
            double dpiScaleX = dpi.DpiScaleX;
            double dpiScaleY = dpi.DpiScaleY;

            // Calculate screen coordinates BEFORE hiding window (in physical pixels)
            int width = (int)Math.Round(_selectedRect.Width * dpiScaleX);
            int height = (int)Math.Round(_selectedRect.Height * dpiScaleY);
            int x = (int)Math.Round((_selectedRect.X + this.Left) * dpiScaleX);
            int y = (int)Math.Round((_selectedRect.Y + this.Top) * dpiScaleY);
            
            // ✨ FIX: Hide all UI elements to avoid capturing them
            SelectionRect.Visibility = Visibility.Collapsed;
            SizeLabel.Visibility = Visibility.Collapsed;
            CrosshairH.Visibility = Visibility.Collapsed;
            CrosshairV.Visibility = Visibility.Collapsed;
            OverlayTop.Visibility = Visibility.Collapsed;
            OverlayBottom.Visibility = Visibility.Collapsed;
            OverlayLeft.Visibility = Visibility.Collapsed;
            OverlayRight.Visibility = Visibility.Collapsed;
            ActionPanel.Visibility = Visibility.Collapsed;
            
            // Force render
            Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
            
            // ✨ FIX: Hide window completely to avoid capturing overlay
            this.Visibility = Visibility.Collapsed;
            
            // Force render and wait for window to hide
            Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
            Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle); // Đợi render hoàn tất thay vì Thread.Sleep
            
            // ✨ IMPROVED: Capture screen with high quality settings
            var bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            
            // Set DPI to match screen DPI for crisp images
            bitmap.SetResolution(96, 96);
            
            using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                // ✨ HIGH QUALITY SETTINGS for sharp images
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                
                graphics.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height), System.Drawing.CopyPixelOperation.SourceCopy);
            }
            
            // Convert to BitmapSource
            var handle = bitmap.GetHbitmap();
            try
            {
                var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    handle,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                
                // ✨ Freeze for better performance and thread safety
                bitmapSource.Freeze();
                return bitmapSource;
            }
            finally
            {
                DeleteObject(handle);
            }
        }

        private void SaveBitmap(BitmapSource bitmap, string filename)
        {
            BitmapEncoder encoder;
            string ext = Path.GetExtension(filename).ToLower();
            
            if (ext == ".jpg" || ext == ".jpeg")
            {
                encoder = new JpegBitmapEncoder { QualityLevel = 95 };
            }
            else
            {
                encoder = new PngBitmapEncoder();
            }
            
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            
            using (var stream = File.Create(filename))
            {
                encoder.Save(stream);
            }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
    }
}
