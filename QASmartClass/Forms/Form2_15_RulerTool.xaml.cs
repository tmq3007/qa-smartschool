using System;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace QASmartTouch.Forms
{
    public partial class Form2_15_RulerTool : Window
    {
        private Form2_MainDashboard _mainDashboard;
        
        // Transform states
        private double _rotation = 0;
        private double _scale = 1.0;
        private bool _isFlipped = false;
        private int _rotateStep = 0; // ✨ Bước xoay 45°
        
        // Original window dimensions
        private const double OriginalWidth = 980;
        private const double OriginalHeight = 120;
        
        // Drawing states - QUY TRÌNH MỚI
        private Point? _drawStartPoint = null;
        private Line? _previewLine = null; // On MainBoard
        private Line? _previewLineOverlay = null; // On PreviewOverlay (ruler window)
        private bool _isDrawing = false;
        private bool _drewFromTopEdge = false; // ✨ Track which edge was used for drawing
        
        // Brush Settings - Thông số bút vẽ đã lưu
        private Color _strokeColor = Color.FromRgb(34, 34, 34);
        private double _strokeThickness = 2.0;
        private PenLineCap _strokeCap = PenLineCap.Round;
        
        // Dragging states
        private bool _isDragging = false;
        private Point _dragStartPoint;
        
        // ✨ Resizable Ruler - NEW
        private double _rulerLengthCm = 30; // Current ruler length in cm
        private const double CM_TO_PIXELS = 32; // 1cm = 32 pixels
        private const double MIN_LENGTH_CM = 10; // Minimum 10cm
        private const double MAX_LENGTH_CM = 50; // Maximum 50cm
        private bool _isResizingLeft = false;
        private bool _isResizingRight = false;
        private Point _resizeStartPoint;
        private double _resizeStartLength;
        
        // ✨ Free Rotation - NEW
        private bool _isRotatingFree = false;
        private Point _rotationCenter;
        
        public Form2_15_RulerTool(Form2_MainDashboard mainDashboard)
        {
            InitializeComponent();
            // QC_4.2_TOUCH_PIPELINE: STEM Window — WPF tự cô lập, KHÔNG cần ApplyTouchIsolation
            QASmartTouch.Helpers.TouchActivationHelper.Apply(this); // QC_4.2_TOUCH_ACTIVATION: Fix "nhấn 2 lần mới kéo được" trên IFP
            _mainDashboard = mainDashboard;
            
            // ✨ Load brush settings từ cấu hình đã lưu
            LoadBrushSettings();
            
            // Generate ruler marks
            GenerateRulerMarks();
            
            // ✨ Set correct window width for initial ruler length (30cm)
            UpdateRulerLength();
            
            // ✨ Center window on screen (both X and Y)
            this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;
            this.Top = (SystemParameters.PrimaryScreenHeight - this.Height) / 2;
            
            // Keyboard shortcuts
            this.KeyDown += Window_KeyDown;
            
            // ✨ Initialize angle display
            txtAngle.Text = "0°";
            
            // ✨ Khởi tạo Clip và layout
            this.Loaded += (s, e) =>
            {
                UpdateRulerClipAndBody();
                UpdateWindowSize(); // Đảm bảo kích thước đúng ngay từ đầu
            };
            
            // ✨ Cập nhật Clip khi Canvas thay đổi kích thước
            RulerCanvas.SizeChanged += (s, e) => UpdateRulerClipAndBody();
            
            // ✅ FIX: Safety cleanup preview line khi Ruler bị đóng giữa chừng
            this.Closing += (s, e) => CleanupPreviewLine();
        }
        
        private void LoadBrushSettings()
        {
            // ✨ Nạp cấu hình bút vẽ từ settings/preferences
            // TODO: Tích hợp với hệ thống Settings của bạn
            // Hiện tại dùng giá trị mặc định:
            _strokeColor = Color.FromRgb(34, 34, 34); // #222222
            _strokeThickness = 2.0;
            _strokeCap = PenLineCap.Round;
            
            // Trong tương lai có thể load từ:
            // _strokeColor = Properties.Settings.Default.StrokeColor;
            // _strokeThickness = Properties.Settings.Default.StrokeThickness;
            // _strokeCap = Properties.Settings.Default.StrokeCap;
        }
        
        private void GenerateRulerMarks()
        {
            // ✨ Use dynamic ruler length
            int totalMm = (int)(_rulerLengthCm * 10); // Convert cm to mm
            double pxPerMm = 3.2; // 1mm = 3.2px (32px per cm / 10)
            double canvasHeight = 60; // ✨ Increased to 60px for maximum gap
            
            // Clear existing marks
            RulerCanvas.Children.Clear();
            
            // Re-add CM label
            var cmLabel = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(153, 153, 153)),
                BorderThickness = new Thickness(0.5),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 2, 4, 2)
            };
            var cmText = new TextBlock
            {
                Text = "CM",
                FontSize = 7,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102))
            };
            cmLabel.Child = cmText;
            Canvas.SetLeft(cmLabel, 4);  // ✨ Moved left 1px more (5 - 1 = 4)
            Canvas.SetTop(cmLabel, 0);   // ✨ Aligned with top edge marks
            RulerCanvas.Children.Add(cmLabel);
            
            for (int i = 0; i <= totalMm; i++)
            {
                double left = 20 + (i * pxPerMm); // 20px offset from left edge
                
                // ✨ DUAL-EDGE MARKS - Top and Bottom
                // Gap in middle (25-35px) for numbers - MAXIMUM SPACE!
                
                // Determine mark heights based on type
                double topMarkHeight, bottomMarkStart;
                double strokeThickness;
                
                // Vạch cm - 21px (reduced 10% from 23px)
                if (i % 10 == 0)
                {
                    topMarkHeight = 21;    // Top edge: 0-21px
                    bottomMarkStart = 39;  // Bottom edge: 39-60px (21px)
                    strokeThickness = 1.5;
                    
                    // ✨ TOP EDGE MARK
                    Line topMark = new Line
                    {
                        X1 = left, Y1 = 0,
                        X2 = left, Y2 = topMarkHeight,
                        Stroke = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                        StrokeThickness = strokeThickness
                    };
                    RulerCanvas.Children.Add(topMark);
                    
                    // ✨ BOTTOM EDGE MARK
                    Line bottomMark = new Line
                    {
                        X1 = left, Y1 = bottomMarkStart,
                        X2 = left, Y2 = canvasHeight,
                        Stroke = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                        StrokeThickness = strokeThickness
                    };
                    RulerCanvas.Children.Add(bottomMark);
                    
                    // ✨ CENTER NUMBER (in the gap 25-35px)
                    int cmValue = i / 10;
                    bool isMajorNumber = (cmValue % 10 == 0); // ✨ Include 0 as major number
                    
                    TextBlock number = new TextBlock
                    {
                        Text = cmValue.ToString(),
                        FontSize = isMajorNumber ? 14.3 : 11,
                        FontWeight = FontWeights.Bold,
                        Foreground = isMajorNumber 
                            ? new SolidColorBrush(Color.FromRgb(220, 53, 69))  // Red
                            : new SolidColorBrush(Color.FromRgb(51, 51, 51)),  // Gray
                        Tag = "RulerNumber",
                        // ✨ Apply transform based on flip state
                        LayoutTransform = new ScaleTransform(1, _isFlipped ? -1 : 1),
                        RenderTransformOrigin = new Point(0.5, 0.5)
                    };
                    
                    double leftOffset = isMajorNumber ? -7 : -5;
                    Canvas.SetLeft(number, left + leftOffset);
                    Canvas.SetTop(number, 23); // ✨ Vertically centered (60px / 2 - font/2 ≈ 23px)
                    RulerCanvas.Children.Add(number);
                }
                // Vạch 5mm - 14px (reduced 10% from 15px)
                else if (i % 5 == 0)
                {
                    topMarkHeight = 14;    // 0-14px
                    bottomMarkStart = 46;  // 46-60px (14px)
                    strokeThickness = 1.0;
                    
                    // TOP EDGE
                    Line topMark = new Line
                    {
                        X1 = left, Y1 = 0,
                        X2 = left, Y2 = topMarkHeight,
                        Stroke = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                        StrokeThickness = strokeThickness
                    };
                    RulerCanvas.Children.Add(topMark);
                    
                    // BOTTOM EDGE
                    Line bottomMark = new Line
                    {
                        X1 = left, Y1 = bottomMarkStart,
                        X2 = left, Y2 = canvasHeight,
                        Stroke = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                        StrokeThickness = strokeThickness
                    };
                    RulerCanvas.Children.Add(bottomMark);
                }
                // Vạch mm - 8px (reduced 10% from 9px)
                else
                {
                    topMarkHeight = 8;     // 0-8px
                    bottomMarkStart = 52;  // 52-60px (8px)
                    strokeThickness = 0.6;
                    
                    // TOP EDGE
                    Line topMark = new Line
                    {
                        X1 = left, Y1 = 0,
                        X2 = left, Y2 = topMarkHeight,
                        Stroke = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                        StrokeThickness = strokeThickness
                    };
                    RulerCanvas.Children.Add(topMark);
                    
                    // BOTTOM EDGE
                    Line bottomMark = new Line
                    {
                        X1 = left, Y1 = bottomMarkStart,
                        X2 = left, Y2 = canvasHeight,
                        Stroke = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                        StrokeThickness = strokeThickness
                    };
                    RulerCanvas.Children.Add(bottomMark);
                }
            }
        }
        
        // ============ MINIMAL MODE & CLIP ============
        
        private void UpdateRulerClipAndBody()
        {
            // ✨ Cập nhật kích thước RulerBody theo Canvas
            var w = RulerCanvas.ActualWidth;
            var h = RulerCanvas.ActualHeight;
            
            if (w > 0 && h > 0)
            {
                // RulerBody theo đúng kích thước canvas
                RulerBody.Width = w;
                RulerBody.Height = h;
                
                // ✨ No clipping needed - bordered design handles rotation
            }
        }
        
        // ============ WINDOW DRAGGING ============
        
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
        
        // ============ CONTROL BUTTONS ============
        
        private void btnMove_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isDragging = true;
                _dragStartPoint = PointToScreen(e.GetPosition(this));

                // ✅ CaptureMouse: mọi MouseMove/MouseUp sẽ đến btnMove dù di chuột nhanh
                if (sender is UIElement el)
                    el.CaptureMouse();

                RulerLayerRoot.CacheMode = new BitmapCache { RenderAtScale = 1 };
                e.Handled = true;
            }
        }

        private void btnMove_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // ✅ Logic xử lý ở đây vì CaptureMouse() route tất cả events về btnMove
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentScreenPoint = PointToScreen(e.GetPosition(this));

                double offsetX = currentScreenPoint.X - _dragStartPoint.X;
                double offsetY = currentScreenPoint.Y - _dragStartPoint.Y;

                this.Left += offsetX;
                this.Top  += offsetY;

                _dragStartPoint = currentScreenPoint;
                e.Handled = true;
            }
        }

        private void btnMove_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;

                if (sender is UIElement el)
                    el.ReleaseMouseCapture();

                RulerLayerRoot.CacheMode = null;
                e.Handled = true;
            }
        }

        // ============ TOUCH MOVE HANDLERS (QC_4.2_TOUCH_PIPELINE) ============
        private int? _moveTouchDeviceId = null;

        private void btnMove_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            if (sender is UIElement el)
            {
                _moveTouchDeviceId = e.TouchDevice.Id;
                _isDragging = true;
                _dragStartPoint = PointToScreen(e.GetTouchPoint(this).Position);

                el.CaptureTouch(e.TouchDevice);

                RulerLayerRoot.CacheMode = new BitmapCache { RenderAtScale = 1 };
                e.Handled = true;
            }
        }

        private void btnMove_TouchMove(object sender, TouchEventArgs e)
        {
            if (_isDragging && _moveTouchDeviceId == e.TouchDevice.Id)
            {
                var currentScreenPoint = PointToScreen(e.GetTouchPoint(this).Position);

                double offsetX = currentScreenPoint.X - _dragStartPoint.X;
                double offsetY = currentScreenPoint.Y - _dragStartPoint.Y;

                this.Left += offsetX;
                this.Top  += offsetY;

                _dragStartPoint = currentScreenPoint;
                e.Handled = true;
            }
        }

        private void btnMove_TouchUp(object sender, TouchEventArgs e)
        {
            if (_moveTouchDeviceId == e.TouchDevice.Id)
            {
                _isDragging = false;
                _moveTouchDeviceId = null;

                if (sender is UIElement el && e.TouchDevice.Captured == el)
                    el.ReleaseTouchCapture(e.TouchDevice);

                RulerLayerRoot.CacheMode = null;
                e.Handled = true;
            }
        }

        private void btnMove_LostTouchCapture(object sender, TouchEventArgs e)
        {
            if (_moveTouchDeviceId == e.TouchDevice.Id)
            {
                _isDragging = false;
                _moveTouchDeviceId = null;
                RulerLayerRoot.CacheMode = null;
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            // Fallback: xử lý trường hợp drag ko qua btnMove (an toàn)
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentScreenPoint = PointToScreen(e.GetPosition(this));
                double offsetX = currentScreenPoint.X - _dragStartPoint.X;
                double offsetY = currentScreenPoint.Y - _dragStartPoint.Y;
                this.Left += offsetX;
                this.Top  += offsetY;
                _dragStartPoint = currentScreenPoint;
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Fallback: đảm bảo drag kết thúc
            if (_isDragging && e.ChangedButton == MouseButton.Left)
            {
                _isDragging = false;
                RulerLayerRoot.CacheMode = null;
            }
        }
        
        private void btnRotate_Click(object sender, RoutedEventArgs e)
        {
            // ✨ Xoay 45° mỗi lần với animation mượt
            _rotateStep = (_rotateStep + 1) % 8;
            _rotation = 45 * _rotateStep;
            
            var anim = new DoubleAnimation
            {
                To = _rotation,
                Duration = TimeSpan.FromMilliseconds(160),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            RulerRotate.BeginAnimation(RotateTransform.AngleProperty, anim);
            
            UpdateNumbersOrientation();
            UpdateWindowSize();
            UpdateRulerClipAndBody(); // Cập nhật Clip sau khi xoay
        }
        
        private void btnFlip_Click(object sender, RoutedEventArgs e)
        {
            _isFlipped = !_isFlipped;
            
            // ✨ Animation cho flip
            double targetScaleY = _isFlipped ? -1 : 1;
            var anim = new DoubleAnimation
            {
                To = targetScaleY,
                Duration = TimeSpan.FromMilliseconds(160),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            
            // ✨ Regenerate marks AFTER flip animation completes
            anim.Completed += (s, args) =>
            {
                GenerateRulerMarks(); // Regenerate with correct orientation
            };
            
            RulerScale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
            
            // ✨ Buttons are now inside bordered container - no need to reposition
        }
        
        private void btnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            _scale = Math.Min(_scale + 0.2, 2.0);
            
            // ✨ Animation cho zoom
            var anim = new DoubleAnimation
            {
                To = _scale,
                Duration = TimeSpan.FromMilliseconds(160),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            RulerScale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            
            UpdateWindowSize();
        }
        
        private void btnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            _scale = Math.Max(_scale - 0.2, 0.5);
            
            // ✨ Animation cho zoom
            var anim = new DoubleAnimation
            {
                To = _scale,
                Duration = TimeSpan.FromMilliseconds(160),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            RulerScale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            
            UpdateWindowSize();
        }
        
        // Removed btnDrawMode_Click - always in draw mode
        
        #region ✨ Resizable Ruler Handlers
        
        // LEFT HANDLE
        private void LeftHandle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isResizingLeft = true;
            _resizeStartPoint = e.GetPosition(this);
            _resizeStartLength = _rulerLengthCm;
            LeftHandle.CaptureMouse();
            e.Handled = true;
        }

        private void LeftHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isResizingLeft) return;
            
            Point currentPoint = e.GetPosition(this);
            double deltaX = currentPoint.X - _resizeStartPoint.X;
            
            // Calculate new length (moving left handle decreases length)
            double newLengthCm = _resizeStartLength - (deltaX / CM_TO_PIXELS);
            
            // Clamp to min/max
            newLengthCm = Math.Max(MIN_LENGTH_CM, Math.Min(MAX_LENGTH_CM, newLengthCm));
            
            // Snap to cm
            newLengthCm = Math.Round(newLengthCm);
            
            if (Math.Abs(newLengthCm - _rulerLengthCm) > 0.1)
            {
                _rulerLengthCm = newLengthCm;
                UpdateRulerLength();
            }
        }

        private void LeftHandle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isResizingLeft = false;
            LeftHandle.ReleaseMouseCapture();
            e.Handled = true;
        }

        // RIGHT HANDLE
        private void RightHandle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isResizingRight = true;
            _resizeStartPoint = e.GetPosition(this);
            _resizeStartLength = _rulerLengthCm;
            RightHandle.CaptureMouse();
            e.Handled = true;
        }

        private void RightHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isResizingRight) return;
            
            Point currentPoint = e.GetPosition(this);
            double deltaX = currentPoint.X - _resizeStartPoint.X;
            
            // Calculate new length (moving right handle increases length)
            double newLengthCm = _resizeStartLength + (deltaX / CM_TO_PIXELS);
            
            // Clamp to min/max
            newLengthCm = Math.Max(MIN_LENGTH_CM, Math.Min(MAX_LENGTH_CM, newLengthCm));
            
            // Snap to cm
            newLengthCm = Math.Round(newLengthCm);
            
            if (Math.Abs(newLengthCm - _rulerLengthCm) > 0.1)
            {
                _rulerLengthCm = newLengthCm;
                UpdateRulerLength();
            }
        }

        private void RightHandle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isResizingRight = false;
            RightHandle.ReleaseMouseCapture();
            e.Handled = true;
        }

        // UPDATE RULER LENGTH
        private void UpdateRulerLength()
        {
            // Update canvas width
            double newWidth = _rulerLengthCm * CM_TO_PIXELS;
            RulerCanvas.Width = newWidth;
            
            // ✨ Window size is now fixed at 1650×1650 for rotation support
            // No need to update window width
            
            // Regenerate ruler marks
            GenerateRulerMarks();
            
            // Update title
            this.Title = $"Thước Kẻ {_rulerLengthCm}cm";
        }
        
        #endregion
        
        private void UpdateWindowSize()
        {
            // ✨ Với SizeToContent, Window tự động co giãn theo nội dung
            // Chỉ cần giữ vị trí trung tâm
            
            // Chờ một chút để Window tự động resize, rồi điều chỉnh vị trí
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                // Căn giữa Window sau khi resize
                this.UpdateLayout();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
        
        private void UpdateNumbersOrientation()
        {
            int count = 0;
            // Tìm tất cả TextBlock chữ số và xoay ngược khi thước bị flip
            foreach (var child in RulerCanvas.Children)
            {
                if (child is TextBlock textBlock && textBlock.Tag?.ToString() == "RulerNumber")
                {
                    count++;
                    if (_isFlipped)
                    {
                        // Xoay ngược chữ số khi thước bị flip
                        var scaleTransform = new ScaleTransform(1, -1);
                        textBlock.LayoutTransform = scaleTransform; // ✨ Use LayoutTransform instead
                        textBlock.RenderTransformOrigin = new Point(0.5, 0.5);
                    }
                    else
                    {
                        // Trở về bình thường
                        textBlock.LayoutTransform = new ScaleTransform(1, 1);
                        textBlock.RenderTransformOrigin = new Point(0.5, 0.5);
                    }
                }
            }
            
            // Debug: Show how many numbers were updated
            System.Diagnostics.Debug.WriteLine($"UpdateNumbersOrientation: Updated {count} numbers, _isFlipped={_isFlipped}");
        }
        
        // ============ DRAWING LOGIC - QUY TRÌNH MỚI ============
        
        private void RulerCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // ✨ Nếu đang drag thước, không vẽ
            if (_isDragging)
            {
                e.Handled = true;
                return;
            }
            
            // ✨ Nếu đang drag thước (chuột trái giữ), không vẽ
            if (e.LeftButton == MouseButtonState.Pressed && this.IsMouseCaptured)
            {
                return; // Đang drag window, không vẽ
            }
            
            // ✨ BƯỚC 1: Bắt đầu vẽ với Mouse Capture
            _drawStartPoint = e.GetPosition(RulerCanvas);
            _isDrawing = true;
            
            // ✨ AUTO-DETECT EDGE: Determine which edge to draw from
            bool useTopEdge = _drawStartPoint.Value.Y < 30; // Top half = top edge (60px / 2 = 30px)
            _drewFromTopEdge = useTopEdge; // ✨ Save for auto-move later
            
            // ✨ Adjust start point to appropriate edge
            Point startOnRuler = new Point(
                _drawStartPoint.Value.X,
                useTopEdge ? 0 : 60  // Top edge (0px) or Bottom edge (60px)
            );
            
            // ✨ Capture mouse để luôn nhận MouseUp kể cả khi kéo ra ngoài
            Mouse.Capture(RulerCanvas);
            
            // ✨ Convert to MainInteractiveBoard coordinates
            Point startOnScreen = RulerCanvas.PointToScreen(startOnRuler);
            Point startOnMain = _mainDashboard.MainInteractiveBoard.PointFromScreen(startOnScreen);
            
            // ✨ Create preview line on MAINBOARD (for parts outside ruler)
            _previewLine = new Line
            {
                X1 = startOnMain.X,
                Y1 = startOnMain.Y,
                X2 = startOnMain.X,
                Y2 = startOnMain.Y,
                Stroke = new SolidColorBrush(Color.FromArgb(220, 255, 152, 0)), // Bright orange
                StrokeThickness = 4,
                StrokeDashArray = new DoubleCollection { 8, 4 },
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            Panel.SetZIndex(_previewLine, 99999);
            _mainDashboard.MainInteractiveBoard.Children.Add(_previewLine);
            
            // ✨ Create preview line OVERLAY on ruler window (for parts under ruler)
            Point startOnWindow = RulerCanvas.PointToScreen(startOnRuler);
            Point startOnOverlay = PreviewOverlay.PointFromScreen(startOnWindow);
            
            _previewLineOverlay = new Line
            {
                X1 = startOnOverlay.X,
                Y1 = startOnOverlay.Y,
                X2 = startOnOverlay.X,
                Y2 = startOnOverlay.Y,
                Stroke = new SolidColorBrush(Color.FromArgb(220, 255, 152, 0)), // Bright orange
                StrokeThickness = 4,
                StrokeDashArray = new DoubleCollection { 8, 4 },
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            PreviewOverlay.Children.Add(_previewLineOverlay);
        }
        
        private void RulerCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            // ✨ Nếu đang drag thước, không vẽ
            if (_isDragging)
            {
                return;
            }
            
            // ✨ FREE ROTATION: Right-click drag
            if (_isRotatingFree && e.RightButton == MouseButtonState.Pressed)
            {
                Point mousePos = e.GetPosition(RulerCanvas);
                double deltaX = mousePos.X - _rotationCenter.X;
                double deltaY = mousePos.Y - _rotationCenter.Y;
                double angle = Math.Atan2(deltaY, deltaX) * 180 / Math.PI;
                SetRotation(angle);
                return; // Don't process drawing while rotating
            }
            
            // ✨ BƯỚC 2: Kéo vẽ realtime
            if (_isDrawing && _previewLine != null && _previewLineOverlay != null && _drawStartPoint.HasValue)
            {
                Point currentPoint = e.GetPosition(RulerCanvas);
                
                // ✨ Chỉ vẽ nếu di chuyển > 3px (tránh nhầm click)
                double distance = Math.Sqrt(
                    Math.Pow(currentPoint.X - _drawStartPoint.Value.X, 2) + 
                    Math.Pow(currentPoint.Y - _drawStartPoint.Value.Y, 2)
                );
                
                if (distance > 3)
                {
                    // ✨ Update preview line on MainBoard
                    Point currentOnScreen = RulerCanvas.PointToScreen(currentPoint);
                    Point currentOnMain = _mainDashboard.MainInteractiveBoard.PointFromScreen(currentOnScreen);
                    _previewLine.X2 = currentOnMain.X;
                    _previewLine.Y2 = currentOnMain.Y;
                    
                    // ✨ Update preview line overlay on ruler window
                    Point currentOnOverlay = PreviewOverlay.PointFromScreen(currentOnScreen);
                    _previewLineOverlay.X2 = currentOnOverlay.X;
                    _previewLineOverlay.Y2 = currentOnOverlay.Y;
                }
            }
        }
        
        private void RulerCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // ✨ BƯỚC 3: Hoàn tất vẽ
            if (_isDrawing && _drawStartPoint.HasValue)
            {
                // Release mouse capture
                Mouse.Capture(null);
                
                // Lấy điểm cuối chính xác
                Point endPoint = e.GetPosition(RulerCanvas);
                
                // Remove preview line - ĐẢM BẢO xóa trước
                CleanupPreviewLine();
                
                // Check if it's not just a click (minimum distance)
                double distance = Math.Sqrt(
                    Math.Pow(endPoint.X - _drawStartPoint.Value.X, 2) + 
                    Math.Pow(endPoint.Y - _drawStartPoint.Value.Y, 2)
                );
                
                if (distance > 5) // Minimum 5px to be considered a line
                {
                    // ✨ Vẽ đường thẳng từ điểm đầu đến điểm cuối (KHÔNG theo chuột)
                    DrawStraightLineOnMainCanvas(_drawStartPoint.Value, endPoint);
                }
                
                // Reset state
                _drawStartPoint = null;
                _isDrawing = false;
            }
        }
        
        private void RulerCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            // ✨ BƯỚC 4: An toàn trạng thái - khi chuột rời canvas
            // Không hủy nếu đang vẽ và đã capture mouse (vẫn nhận MouseUp)
            // Chỉ cleanup khi chưa capture hoặc user ESC
        }
        
        private void CleanupPreviewLine()
        {
            // ✨ Method riêng để đảm bảo preview lines luôn bị xóa
            if (_previewLine != null)
            {
                _mainDashboard.MainInteractiveBoard.Children.Remove(_previewLine);
                _previewLine = null;
            }
            
            if (_previewLineOverlay != null)
            {
                PreviewOverlay.Children.Remove(_previewLineOverlay);
                _previewLineOverlay = null;
            }
        }
        
        private void DrawStraightLineOnMainCanvas(Point start, Point end)
        {
            try
            {
                // ✨ DEBUG: Check if MainInteractiveBoard is available
                if (_mainDashboard == null)
                {
                    MessageBox.Show("Error: _mainDashboard is null!", "Debug", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                if (_mainDashboard.MainInteractiveBoard == null)
                {
                    MessageBox.Show("Error: MainInteractiveBoard is null!", "Debug", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // ✨ LUÔN VẼ ĐƯỜNG THẲNG TỪ ĐIỂM ĐẦU ĐẾN ĐIỂM CUỐI
                // Không quan tâm đến quỹ đạo chuột di chuyển
                
                // ✨ ALWAYS use Screen coordinates (works with rotation!)
                // TransformToVisual fails when ruler is rotated
                Point startOnScreen = RulerCanvas.PointToScreen(start);
                Point endOnScreen = RulerCanvas.PointToScreen(end);
                Point startOnMain = _mainDashboard.MainInteractiveBoard.PointFromScreen(startOnScreen);
                Point endOnMain = _mainDashboard.MainInteractiveBoard.PointFromScreen(endOnScreen);
                
                // ✨ DEBUG: Show transformed coordinates
                System.Diagnostics.Debug.WriteLine($"Screen transform: Start({startOnMain.X:F0}, {startOnMain.Y:F0}), End({endOnMain.X:F0}, {endOnMain.Y:F0})");
                
                // ✨ Tạo Line với THÔNG SỐ ĐÃ LƯU
                Line line = new Line
                {
                    X1 = startOnMain.X,
                    Y1 = startOnMain.Y,
                    X2 = endOnMain.X,
                    Y2 = endOnMain.Y,
                    Stroke = new SolidColorBrush(_strokeColor), // ✨ Màu đã lưu
                    StrokeThickness = _strokeThickness,          // ✨ Độ dày đã lưu
                    StrokeStartLineCap = _strokeCap,             // ✨ LineCap đã lưu
                    StrokeEndLineCap = _strokeCap,
                    SnapsToDevicePixels = true                   // ✨ Nét sắc nét
                };
                
                // ✨ Tùy chọn: EdgeMode cho nét sắc hơn
                // RenderOptions.SetEdgeMode(line, EdgeMode.Aliased);
                
                // Add to main canvas
                _mainDashboard.MainInteractiveBoard.Children.Add(line);
                
                // ✅ Record for undo/redo
                _mainDashboard.RecordToolDrawAction(line, "Ruler: draw line");
                
                // ✨ Tự động dịch thước ra ngoài để thấy rõ đường vẽ (DISABLED)
                // AutoMoveRulerAway();
                
                // ✨ Hiển thị thông báo nhỏ
                ShowTemporaryMessage("✓ Đã vẽ");
                
                // Play sound feedback
                SystemSounds.Asterisk.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi vẽ đường: {ex.Message}\n\nStack trace: {ex.StackTrace}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void AutoMoveRulerAway()
        {
            // ✨ Different move distances based on which edge was used
            // Top edge → Move down 20px
            // Bottom edge → Move up 20px
            double moveDistance = 20; // ✨ Same distance for both edges
            double newTop;
            
            if (_drewFromTopEdge)
            {
                // Vẽ từ cạnh trên → Dịch XUỐNG DƯỚI 20px
                newTop = this.Top + moveDistance;
                
                // Đảm bảo không vượt ra ngoài mép dưới màn hình
                double screenHeight = SystemParameters.PrimaryScreenHeight;
                if (newTop + this.Height > screenHeight)
                {
                    newTop = screenHeight - this.Height - 50; // Cách mép dưới 50px
                }
            }
            else
            {
                // Vẽ từ cạnh dưới → Dịch LÊN TRÊN 20px
                newTop = this.Top - moveDistance;
                
                // Đảm bảo không vượt ra ngoài mép trên màn hình
                if (newTop < 0)
                {
                    newTop = 50; // Tối thiểu 50px từ mép trên
                }
            }
            
            this.Top = newTop;
        }
        
        private void ShowTemporaryMessage(string message)
        {
            // Tạo TextBlock thông báo tạm thời
            TextBlock notification = new TextBlock
            {
                Text = message,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69)), // Màu xanh lá
                Background = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)), // Nền trắng mờ
                Padding = new Thickness(12, 6, 12, 6),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            // Thêm vào Grid chính (tạm thời)
            Grid mainGrid = (Grid)this.Content;
            Grid.SetRow(notification, 0);
            Grid.SetColumn(notification, 0);
            Grid.SetColumnSpan(notification, 3);
            mainGrid.Children.Add(notification);
            
            // Tự động xóa sau 1 giây
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += (s, e) =>
            {
                mainGrid.Children.Remove(notification);
                timer.Stop();
            };
            timer.Start();
        }
        
        // ============ KEYBOARD SHORTCUTS ============
        
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.R:
                    // ✨ Rotation shortcuts
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        // Ctrl+R: Reset to 0°
                        _rotation = 0;
                        RotateRuler(0);
                    }
                    else if (Keyboard.Modifiers == ModifierKeys.Shift)
                    {
                        // Shift+R: Rotate -45° (counter-clockwise)
                        RotateRuler(-45);
                    }
                    else
                    {
                        // R: Rotate +45° (clockwise)
                        RotateRuler(45);
                    }
                    break;
                case Key.OemPlus:
                case Key.Add:
                    btnZoomIn_Click(null, null);
                    break;
                case Key.OemMinus:
                case Key.Subtract:
                    btnZoomOut_Click(null, null);
                    break;
                case Key.Escape:
                    // ✨ ESC để hủy thao tác vẽ hoặc đóng window
                    if (_isDrawing)
                    {
                        CancelDrawing();
                    }
                    else
                    {
                        this.Close();
                    }
                    break;
            }
        }
        
        private void CancelDrawing()
        {
            // ✨ Hủy thao tác vẽ (ESC hoặc Cancel)
            Mouse.Capture(null);
            CleanupPreviewLine();
            _drawStartPoint = null;
            _isDrawing = false;
        }
        
        #region ✨ Rotation Methods
        
        // Rotation button handlers
        private void btnRotateLeft_Click(object sender, RoutedEventArgs e)
        {
            RotateRuler(-45);
        }

        private void btnRotateRight_Click(object sender, RoutedEventArgs e)
        {
            RotateRuler(45);
        }

        private void btnRotate90_Click(object sender, RoutedEventArgs e)
        {
            RotateRuler(90);
        }

        private void btnResetAngle_Click(object sender, RoutedEventArgs e)
        {
            _rotation = 0;
            RotateRuler(0);
        }
        
        // Precision rotation ±1°
        private void btnRotateMinus1_Click(object sender, RoutedEventArgs e)
        {
            RotateRuler(-1);
        }

        private void btnRotatePlus1_Click(object sender, RoutedEventArgs e)
        {
            RotateRuler(1);
        }
        
        // Rotate ruler by angle increment with animation
        private void RotateRuler(double angleIncrement)
        {
            // ✨ Get current angle from transform (not from _rotation variable)
            double currentAngle = RulerRotate.Angle;
            
            // ✨ Calculate new angle
            double newAngle = currentAngle + angleIncrement;
            
            // ✨ Update _rotation for tracking (normalized 0-360)
            _rotation = newAngle;
            if (_rotation >= 360) _rotation -= 360;
            if (_rotation < 0) _rotation += 360;
            
            // ✨ Animate from current to new angle (shortest path, no normalization)
            var anim = new DoubleAnimation
            {
                From = currentAngle,
                To = newAngle,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            RulerRotate.BeginAnimation(RotateTransform.AngleProperty, anim);
            
            // Update angle display (normalized)
            txtAngle.Text = $"{_rotation:F0}°";
        }

        // Set exact rotation (for free rotation)
        private void SetRotation(double angle)
        {
            _rotation = angle;
            if (_rotation < 0) _rotation += 360;
            if (_rotation >= 360) _rotation -= 360;
            
            RulerRotate.Angle = _rotation;
            txtAngle.Text = $"{_rotation:F0}°";
        }
        
        // Mouse wheel rotation (±5° per scroll)
        private void RulerCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.None)
            {
                double angleIncrement = e.Delta > 0 ? 5 : -5;
                RotateRuler(angleIncrement);
                e.Handled = true;
            }
        }
        
        // Right-click drag rotation
        private void RulerCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isDrawing) // Don't rotate while drawing
            {
                _isRotatingFree = true;
                _rotationCenter = new Point(RulerCanvas.ActualWidth / 2, RulerCanvas.ActualHeight / 2);
                RulerCanvas.CaptureMouse();
                e.Handled = true;
            }
        }

        private void RulerCanvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isRotatingFree = false;
            RulerCanvas.ReleaseMouseCapture();
        }
        
        // Opacity slider handler
        private void opacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (RulerBody != null && txtOpacity != null)
            {
                // ✅ Only update RulerBody opacity (white background)
                // Control panel and ruler marks stay fully visible (100% opacity)
                RulerBody.Opacity = e.NewValue;
                
                // Update percentage display
                int percentage = (int)(e.NewValue * 100);
                txtOpacity.Text = $"{percentage}%";
            }
        }
        
        // Close button handler
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        
        #endregion
    }
}
