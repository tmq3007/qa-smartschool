using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace QASmartTouch.Forms
{
    public partial class AreaSelectionWindow : Window
    {
        private Point _startPoint;
        private bool _isSelecting = false;
        private Rect _selectedArea;

        public Rect SelectedArea => _selectedArea;
        public bool WasCancelled { get; private set; } = false;

        public AreaSelectionWindow()
        {
            InitializeComponent();
            // NV 4.3: Đã xóa dead code Canvas.SetLeft(InstructionBorder) — InstructionBorder nằm trong Grid, không phải Canvas
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Do not initiate selection if clicking the cancel button
            if (e.OriginalSource is DependencyObject dep && (dep == btnCancelSelection || btnCancelSelection.IsAncestorOf(dep)))
            {
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _startPoint = e.GetPosition(this);
                _isSelecting = true;
                
                SelectionRect.Visibility = Visibility.Visible;
                InstructionBorder.Visibility = Visibility.Collapsed;

                // NV 4.4: Ẩn crosshair khi bắt đầu kéo chọn
                CrosshairH.Visibility = Visibility.Collapsed;
                CrosshairV.Visibility = Visibility.Collapsed;

                // NV 4.2: Khởi tạo spotlight overlays
                OverlayBottom.Visibility = Visibility.Visible;
                OverlayLeft.Visibility = Visibility.Visible;
                OverlayRight.Visibility = Visibility.Visible;

                Canvas.SetLeft(SelectionRect, _startPoint.X);
                Canvas.SetTop(SelectionRect, _startPoint.Y);
                SelectionRect.Width = 0;
                SelectionRect.Height = 0;
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            var currentPoint = e.GetPosition(this);

            if (_isSelecting)
            {
                var x = Math.Min(_startPoint.X, currentPoint.X);
                var y = Math.Min(_startPoint.Y, currentPoint.Y);
                var width = Math.Abs(currentPoint.X - _startPoint.X);
                var height = Math.Abs(currentPoint.Y - _startPoint.Y);
                
                Canvas.SetLeft(SelectionRect, x);
                Canvas.SetTop(SelectionRect, y);
                SelectionRect.Width = width;
                SelectionRect.Height = height;

                // NV 4.1: Cập nhật SizeLabel real-time
                txtSize.Text = $"{(int)width} × {(int)height}";
                SizeLabel.Visibility = Visibility.Visible;
                Canvas.SetLeft(SizeLabel, x + width + 10);
                Canvas.SetTop(SizeLabel, y - 5);

                // NV 4.2: Cập nhật spotlight overlay
                UpdateSpotlightOverlay(x, y, width, height);
            }
            else
            {
                // NV 4.4: Hiển thị crosshair theo dõi chuột (trước khi bắt đầu chọn)
                CrosshairH.Visibility = Visibility.Visible;
                CrosshairV.Visibility = Visibility.Visible;

                CrosshairH.X1 = 0;
                CrosshairH.Y1 = currentPoint.Y;
                CrosshairH.X2 = this.ActualWidth;
                CrosshairH.Y2 = currentPoint.Y;

                CrosshairV.X1 = currentPoint.X;
                CrosshairV.Y1 = 0;
                CrosshairV.X2 = currentPoint.X;
                CrosshairV.Y2 = this.ActualHeight;
            }
        }

        /// <summary>
        /// NV 4.2: Cập nhật 4 overlay rectangles tạo hiệu ứng spotlight xung quanh vùng chọn
        /// </summary>
        private void UpdateSpotlightOverlay(double x, double y, double w, double h)
        {
            double screenW = this.ActualWidth;
            double screenH = this.ActualHeight;

            // Top overlay: từ trên cùng đến cạnh trên selection
            OverlayTop.Margin = new Thickness(0);
            OverlayTop.VerticalAlignment = VerticalAlignment.Top;
            OverlayTop.HorizontalAlignment = HorizontalAlignment.Stretch;
            OverlayTop.Height = Math.Max(0, y);

            // Bottom overlay: từ cạnh dưới selection đến dưới cùng
            OverlayBottom.Margin = new Thickness(0, y + h, 0, 0);
            OverlayBottom.VerticalAlignment = VerticalAlignment.Top;
            OverlayBottom.HorizontalAlignment = HorizontalAlignment.Stretch;
            OverlayBottom.Height = Math.Max(0, screenH - y - h);

            // Left overlay: từ trái đến cạnh trái selection (chỉ trong vùng giữa)
            OverlayLeft.Margin = new Thickness(0, y, 0, 0);
            OverlayLeft.VerticalAlignment = VerticalAlignment.Top;
            OverlayLeft.HorizontalAlignment = HorizontalAlignment.Left;
            OverlayLeft.Width = Math.Max(0, x);
            OverlayLeft.Height = Math.Max(0, h);

            // Right overlay: từ cạnh phải selection đến phải (chỉ trong vùng giữa)
            OverlayRight.Margin = new Thickness(x + w, y, 0, 0);
            OverlayRight.VerticalAlignment = VerticalAlignment.Top;
            OverlayRight.HorizontalAlignment = HorizontalAlignment.Left;
            OverlayRight.Width = Math.Max(0, screenW - x - w);
            OverlayRight.Height = Math.Max(0, h);
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelecting)
            {
                _isSelecting = false;
                
                var currentPoint = e.GetPosition(this);
                
                // Tính góc trái-trên và phải-dưới trong tọa độ window (WPF DIPs)
                double x1 = Math.Min(_startPoint.X, currentPoint.X);
                double y1 = Math.Min(_startPoint.Y, currentPoint.Y);
                double x2 = Math.Max(_startPoint.X, currentPoint.X);
                double y2 = Math.Max(_startPoint.Y, currentPoint.Y);
                
                double width  = x2 - x1;
                double height = y2 - y1;
                
                // Minimum size check
                if (width > 10 && height > 10)
                {
                    // Dùng PointToScreen để chuyển từa WPF coords → physical screen pixels
                    // Hàm này tự xử lý: window offset (incl. shadow border) + DPI scaling
                    var physTopLeft     = this.PointToScreen(new System.Windows.Point(x1, y1));
                    var physBottomRight = this.PointToScreen(new System.Windows.Point(x2, y2));
                    
                    _selectedArea = new Rect(
                        physTopLeft.X,
                        physTopLeft.Y,
                        physBottomRight.X - physTopLeft.X,
                        physBottomRight.Y - physTopLeft.Y);
                    
                    System.Diagnostics.Debug.WriteLine(
                        $"📹 WPF selection: ({x1:F0},{y1:F0}) → ({x2:F0},{y2:F0})");
                    System.Diagnostics.Debug.WriteLine(
                        $"📬 Physical screen: X={_selectedArea.X:F0}, Y={_selectedArea.Y:F0}," +
                        $" W={_selectedArea.Width:F0}, H={_selectedArea.Height:F0}");
                    
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    // NV 3.3: Toast thông báo vùng chọn quá nhỏ
                    ShowSelectionTooSmallToast();

                    // Reset hiển thị
                    SelectionRect.Visibility = Visibility.Collapsed;
                    SizeLabel.Visibility = Visibility.Collapsed;
                    InstructionBorder.Visibility = Visibility.Visible;

                    // Reset spotlight — quay lại overlay toàn màn hình
                    OverlayTop.Height = double.NaN; // Auto
                    OverlayTop.VerticalAlignment = VerticalAlignment.Stretch;
                    OverlayBottom.Visibility = Visibility.Collapsed;
                    OverlayLeft.Visibility = Visibility.Collapsed;
                    OverlayRight.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// NV 3.3: Hiển thị thông báo tạm thời khi vùng chọn quá nhỏ (< 10×10 pixel)
        /// Pattern lấy từ AnnotationOverlay.ShowToast (dòng 582-625)
        /// </summary>
        private void ShowSelectionTooSmallToast()
        {
            var toast = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E65100")),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(16, 8, 16, 8),
                Opacity = 0.92,
                IsHitTestVisible = false
            };

            var txt = new TextBlock
            {
                Text = "⚠️ Vùng chọn quá nhỏ. Hãy kéo rộng hơn.",
                Foreground = Brushes.White,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold
            };
            toast.Child = txt;

            // Đặt giữa-dưới màn hình
            double screenW = this.ActualWidth;
            double screenH = this.ActualHeight;
            Canvas.SetLeft(toast, screenW / 2 - 180);
            Canvas.SetTop(toast, screenH - 120);
            Canvas.SetZIndex(toast, 9999);

            SelectionCanvas.Children.Add(toast);

            // Tự xóa sau 2 giây
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            timer.Tick += (s, ev) =>
            {
                SelectionCanvas.Children.Remove(toast);
                timer.Stop();
            };
            timer.Start();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                WasCancelled = true;
                this.DialogResult = false;
                this.Close();
            }
        }

        private void btnCancelSelection_Click(object sender, RoutedEventArgs e)
        {
            WasCancelled = true;
            this.DialogResult = false;
            this.Close();
        }
    }
}
