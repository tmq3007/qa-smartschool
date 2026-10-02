using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace QASmartTouch.Forms
{
    public partial class Form2_19_CircleDrawingTool : Window
    {
        #region Fields

        // MainDashboard reference
        private Form2_MainDashboard? _mainDashboard;

        // Circle state (in cm)
        private double _currentRadiusCm = 5.0; // Default 5cm

        // Conversion: 1cm = 37.795px (96 DPI)
        private const double CM_TO_PX = 37.795;

        // Circle center position
        private Point _circleCenter;

        // Preview circle and elements
        private Ellipse? _previewCircle;
        private Ellipse? _centerPoint;
        private Line? _diameterLine;
        private Polygon? _leftArrow;
        private Polygon? _rightArrow;
        private Border? _sizeLabelBorder;
        private TextBlock? _sizeLabel;

        // Handles
        private Ellipse? _topHandle;
        private Ellipse? _rightHandle;
        private Ellipse? _bottomHandle;
        private Ellipse? _leftHandle;

        // Control panel
        private Border? _controlPanel;
        private TextBox? _radiusInput;

        // Drag states
        private bool _isDraggingHandle = false;
        private Ellipse? _activeHandle = null;
        
        private bool _isDraggingCircle = false;
        private Point _dragStartPoint;

        // Control panel drag state
        private bool _isDraggingPanel = false;
        private Point _panelDragStart;

        // Stroke settings
        private Color _strokeColor = Colors.Black;
        private double _strokeThickness = 2.0;

        #endregion

        #region Constructor

        public Form2_19_CircleDrawingTool()
        {
            InitializeComponent();
            QASmartTouch.Helpers.TouchActivationHelper.Apply(this); // QC_4.2_TOUCH_ACTIVATION: Fix "nhấn 2 lần mới kéo được" trên IFP
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_mainDashboard == null) return;

            // ✅ QC_4.2_TOUCH_ACTIVATION: Giữ cửa sổ ảo không chiếm taskbar mà không đổi WindowState = Minimized
            // Tránh việc hệ điều hành Windows thu hồi trạng thái Active/Focus của ứng dụng
            this.ShowInTaskbar = false;

            // Set initial circle center (middle of screen)
            _circleCenter = new Point(
                _mainDashboard.MainInteractiveBoard.ActualWidth / 2,
                _mainDashboard.MainInteractiveBoard.ActualHeight / 2
            );

            // Create all UI on canvas
            CreateUIOnCanvas();

            // ✅ FIX: Đóng cửa sổ ảo ngay sau khi đã nhúng toàn bộ UI lên Canvas của MainDashboard
            // Giải phóng Window handle, chỉ giữ lại các đối tượng trên Canvas
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
            {
                this.Visibility = Visibility.Collapsed;
                // ✅ QC_4.2_TOUCH_ACTIVATION: Kích hoạt lại MainDashboard ngay lập tức
                // Đảm bảo MainDashboard luôn Active và có Keyboard Focus, loại bỏ hoàn toàn hiện tượng nuốt cú chạm đầu tiên
                try
                {
                    _mainDashboard?.Activate();
                    _mainDashboard?.Focus();
                }
                catch { }
            }));
        }

        #endregion

        #region Create UI

        private void CreateUIOnCanvas()
        {
            if (_mainDashboard == null) return;

            // Create preview circle
            CreatePreviewCircle();

            // Create control panel next to circle
            CreateControlPanel();
        }

        private void CreatePreviewCircle()
        {
            if (_mainDashboard == null) return;

            double radiusPx = _currentRadiusCm * CM_TO_PX;

            // Preview circle
            _previewCircle = new Ellipse
            {
                Width = radiusPx * 2,
                Height = radiusPx * 2,
                Stroke = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                StrokeThickness = 3,
                Fill = Brushes.Transparent,
                StrokeDashArray = new DoubleCollection { 5, 3 }
            };
            Canvas.SetLeft(_previewCircle, _circleCenter.X - radiusPx);
            Canvas.SetTop(_previewCircle, _circleCenter.Y - radiusPx);
            Panel.SetZIndex(_previewCircle, 10000); // Luôn ở trên cùng
            _mainDashboard.MainInteractiveBoard.Children.Add(_previewCircle);

            // Center point (draggable)
            _centerPoint = new Ellipse
            {
                Width = 16,
                Height = 16,
                Fill = new SolidColorBrush(Color.FromRgb(255, 87, 34)),
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Cursor = Cursors.SizeAll,
                Tag = "DragHandle" // ✅ FIX: Block drawing mode stroke leakage
            };
            Canvas.SetLeft(_centerPoint, _circleCenter.X - 8);
            Canvas.SetTop(_centerPoint, _circleCenter.Y - 8);
            Panel.SetZIndex(_centerPoint, 10001); // Cao hơn circle
            
            // ✅ Touch/Stylus Fast-Response & Leakage Prevention
            Stylus.SetIsPressAndHoldEnabled(_centerPoint, false);
            _centerPoint.MouseLeftButtonDown += CenterPoint_MouseDown;
            _centerPoint.MouseMove += CenterPoint_MouseMove;
            _centerPoint.MouseLeftButtonUp += CenterPoint_MouseUp;
            // ✅ AUD-02 FIX: Chuyển từ anonymous lambda sang named method để có thể unsubscribe
            _centerPoint.TouchDown += CenterPoint_TouchDown;
            _centerPoint.TouchMove += CenterPoint_TouchMove;
            _centerPoint.TouchUp += CenterPoint_TouchUp;
            _centerPoint.StylusDown += CenterPoint_StylusDown;
            _centerPoint.StylusMove += CenterPoint_StylusMove;
            _centerPoint.StylusUp += CenterPoint_StylusUp;
            
            _mainDashboard.MainInteractiveBoard.Children.Add(_centerPoint);

            // Diameter line
            _diameterLine = new Line
            {
                X1 = _circleCenter.X - radiusPx,
                Y1 = _circleCenter.Y,
                X2 = _circleCenter.X + radiusPx,
                Y2 = _circleCenter.Y,
                Stroke = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 3, 2 },
                Tag = "DragHandle" // ✅ FIX: Chặn stroke leakage khi chạm vào nét đứt đường kính
            };
            Panel.SetZIndex(_diameterLine, 10000);
            _mainDashboard.MainInteractiveBoard.Children.Add(_diameterLine);

            // Arrows
            _leftArrow = new Polygon
            {
                Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                Points = new PointCollection
                {
                    new Point(_circleCenter.X - radiusPx, _circleCenter.Y),
                    new Point(_circleCenter.X - radiusPx + 5, _circleCenter.Y - 3),
                    new Point(_circleCenter.X - radiusPx + 5, _circleCenter.Y + 3)
                },
                Tag = "DragHandle" // ✅ FIX: Chặn stroke leakage khi chạm vào mũi tên trái
            };
            Panel.SetZIndex(_leftArrow, 10000);
            _mainDashboard.MainInteractiveBoard.Children.Add(_leftArrow);

            _rightArrow = new Polygon
            {
                Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                Points = new PointCollection
                {
                    new Point(_circleCenter.X + radiusPx, _circleCenter.Y),
                    new Point(_circleCenter.X + radiusPx - 5, _circleCenter.Y - 3),
                    new Point(_circleCenter.X + radiusPx - 5, _circleCenter.Y + 3)
                },
                Tag = "DragHandle" // ✅ FIX: Chặn stroke leakage khi chạm vào mũi tên phải
            };
            Panel.SetZIndex(_rightArrow, 10000);
            _mainDashboard.MainInteractiveBoard.Children.Add(_rightArrow);

            // Size label
            _sizeLabel = new TextBlock
            {
                Text = $"⌀ {_currentRadiusCm * 2:F1}cm",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
            };

            _sizeLabelBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(224, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(5, 2, 5, 2),
                Child = _sizeLabel,
                Tag = "DragHandle"
            };
            Canvas.SetLeft(_sizeLabelBorder, _circleCenter.X - 30);
            Canvas.SetTop(_sizeLabelBorder, _circleCenter.Y - radiusPx - 30);
            Panel.SetZIndex(_sizeLabelBorder, 10001); // Cao hơn circle
            _mainDashboard.MainInteractiveBoard.Children.Add(_sizeLabelBorder);

            // Handles
            CreateHandles(radiusPx);
        }

        private void CreateHandles(double radiusPx)
        {
            if (_mainDashboard == null) return;

            _topHandle = CreateHandle();
            Canvas.SetLeft(_topHandle, _circleCenter.X - 8);
            Canvas.SetTop(_topHandle, _circleCenter.Y - radiusPx - 8);
            Panel.SetZIndex(_topHandle, 10001);
            _mainDashboard.MainInteractiveBoard.Children.Add(_topHandle);

            _rightHandle = CreateHandle();
            Canvas.SetLeft(_rightHandle, _circleCenter.X + radiusPx - 8);
            Canvas.SetTop(_rightHandle, _circleCenter.Y - 8);
            Panel.SetZIndex(_rightHandle, 10001);
            _mainDashboard.MainInteractiveBoard.Children.Add(_rightHandle);

            _bottomHandle = CreateHandle();
            Canvas.SetLeft(_bottomHandle, _circleCenter.X - 8);
            Canvas.SetTop(_bottomHandle, _circleCenter.Y + radiusPx - 8);
            Panel.SetZIndex(_bottomHandle, 10001);
            _mainDashboard.MainInteractiveBoard.Children.Add(_bottomHandle);

            _leftHandle = CreateHandle();
            Canvas.SetLeft(_leftHandle, _circleCenter.X - radiusPx - 8);
            Canvas.SetTop(_leftHandle, _circleCenter.Y - 8);
            Panel.SetZIndex(_leftHandle, 10001);
            _mainDashboard.MainInteractiveBoard.Children.Add(_leftHandle);
        }

        private Ellipse CreateHandle()
        {
            var handle = new Ellipse
            {
                Width = 16,
                Height = 16,
                Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Cursor = Cursors.Hand,
                Tag = "DragHandle"
            };

            Stylus.SetIsPressAndHoldEnabled(handle, false);
            handle.MouseLeftButtonDown += Handle_MouseDown;
            handle.MouseMove += Handle_MouseMove;
            handle.MouseLeftButtonUp += Handle_MouseUp;

            // ✅ AUD-02 FIX: Chuyển từ anonymous lambda sang named method để có thể unsubscribe
            handle.TouchDown += Handle_TouchDown;
            handle.TouchMove += Handle_TouchMove;
            handle.TouchUp += Handle_TouchUp;
            handle.StylusDown += Handle_StylusDown;
            handle.StylusMove += Handle_StylusMove;
            handle.StylusUp += Handle_StylusUp;

            return handle;
        }

        private void CreateControlPanel()
        {
            if (_mainDashboard == null) return;

            // Main stack panel
            var mainStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Background = Brushes.White
            };

            // Row 1: Control panel
            var controlRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10, 10, 10, 5)
            };

            // Radius label
            controlRow.Children.Add(new TextBlock
            {
                Text = "Bán kính:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0),
                FontSize = 12
            });

            // Radius input
            _radiusInput = new TextBox
            {
                Text = _currentRadiusCm.ToString("F1"),
                Width = 50,
                Height = 28,
                VerticalContentAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontSize = 12,
                Margin = new Thickness(0, 0, 5, 0)
            };
            _radiusInput.PreviewKeyDown += RadiusInput_KeyDown;
            _radiusInput.GotFocus += (s, e) => _radiusInput.SelectAll();
            controlRow.Children.Add(_radiusInput);

            // cm label
            controlRow.Children.Add(new TextBlock
            {
                Text = "cm",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                FontSize = 12,
                FontWeight = FontWeights.Bold
            });

            // Draw button
            var btnDraw = CreateStandardButton("✓ Vẽ", Color.FromRgb(76, 175, 80), 50);
            btnDraw.Click += btnDraw_Click;
            btnDraw.Margin = new Thickness(0, 0, 5, 0);
            QASmartTouch.Helpers.TouchActivationHelper.WireButton(btnDraw);
            controlRow.Children.Add(btnDraw);

            // Move button (chuẩn như thước kẻ Form2_15_RulerTool)
            var btnMove = CreateStandardButton("✥", Color.FromRgb(102, 126, 234), 32); // #667EEA
            btnMove.ToolTip = "Di chuyển";
            btnMove.FontSize = 14;
            btnMove.FontWeight = FontWeights.Bold;
            btnMove.Cursor = Cursors.SizeAll;
            btnMove.Margin = new Thickness(0, 0, 5, 0);
            btnMove.Tag = "DragHandle"; // ✅ FIX: Prevent drawing mode stroke leakage when dragging compass

            // ✅ FIX: Chỉ dùng DUY NHẤT Mouse handler + PointToScreen() (giống Form2_15_RulerTool)
            // LOẠI BỎ hoàn toàn Touch & Stylus handler thừa trên btnMove.
            // LÝ DO: Trên màn hình cảm ứng SMART TOUCH, Windows tự động quảng bá (promote)
            //   Touch → Mouse. Nếu đăng ký cả Touch + Mouse đồng thời, 2 luồng sự kiện
            //   cùng gọi Panel_MoveDrag với tọa độ lệch nhau → gây Coordinate Feedback Loop
            //   khiến Compa "nháy nháy" tại chỗ mà không di chuyển được.
            Stylus.SetIsPressAndHoldEnabled(btnMove, false);
            btnMove.PreviewMouseDown += btnMove_PreviewMouseDown;
            btnMove.PreviewMouseMove += btnMove_PreviewMouseMove;
            btnMove.PreviewMouseUp += btnMove_PreviewMouseUp;
            // QC_4.2_TOUCH_PIPELINE: Hỗ trợ Touch trực tiếp trên IFP, chặn gesture delay và coordinate loop
            btnMove.PreviewTouchDown += btnMove_PreviewTouchDown;
            btnMove.TouchMove += btnMove_TouchMove;
            btnMove.TouchUp += btnMove_TouchUp;
            btnMove.LostTouchCapture += btnMove_LostTouchCapture;

            controlRow.Children.Add(btnMove);

            // Close button
            var btnClose = CreateStandardButton("✕", Color.FromRgb(255, 107, 107), 32);
            btnClose.Click += btnClose_Click;
            btnClose.FontSize = 13;
            btnClose.FontWeight = FontWeights.Bold;
            QASmartTouch.Helpers.TouchActivationHelper.WireButton(btnClose);
            controlRow.Children.Add(btnClose);

            mainStack.Children.Add(controlRow);

            // Row 2: Quick buttons
            var quickRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10, 0, 10, 10)
            };

            quickRow.Children.Add(CreateQuickButton("2.5cm", 2.5));
            quickRow.Children.Add(CreateQuickButton("5cm", 5.0));
            quickRow.Children.Add(CreateQuickButton("7.5cm", 7.5));
            quickRow.Children.Add(CreateQuickButton("10cm", 10.0));
            quickRow.Children.Add(CreateQuickButton("15cm", 15.0));

            mainStack.Children.Add(quickRow);

            // Wrap in border
            _controlPanel = new Border
            {
                Child = mainStack,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(225, 232, 237)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Tag = "DragHandle" // ✅ FIX: Prevent drawing mode stroke leakage on control panel
            };

            _controlPanel.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Color.FromRgb(204, 204, 204),
                Direction = 270,
                ShadowDepth = 3,
                BlurRadius = 15,
                Opacity = 0.3
            };

            // Position next to circle (to the right)
            double radiusPx = _currentRadiusCm * CM_TO_PX;
            Canvas.SetLeft(_controlPanel, _circleCenter.X + radiusPx + 20);
            Canvas.SetTop(_controlPanel, _circleCenter.Y - 75);
            Panel.SetZIndex(_controlPanel, 10002); // Cao nhất

            _mainDashboard.MainInteractiveBoard.Children.Add(_controlPanel);

            // ✅ QC_4.2_TOUCH_ACTIVATION: Quét toàn bộ control panel để gắn pipeline cảm ứng (bỏ qua btnMove để bảo toàn kéo thả)
            QASmartTouch.Helpers.TouchActivationHelper.WireAllInteractiveControls(_controlPanel, btnMove);
        }

        private Button CreateStandardButton(string content, Color bgColor, double width)
        {
            var button = new Button
            {
                Content = content,
                Width = width,
                Height = 32, // Chuẩn 32px
                Background = new SolidColorBrush(bgColor),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 12,
                Cursor = Cursors.Hand,
                Focusable = false // ✅ QC_4.2_TOUCH_ACTIVATION: Vô hiệu hóa Focusable để không nuốt cú chạm đầu tiên trên IFP
            };
            Stylus.SetIsPressAndHoldEnabled(button, false);

            button.Template = new ControlTemplate(typeof(Button))
            {
                VisualTree = CreateStandardButtonTemplate(bgColor)
            };

            return button;
        }

        private FrameworkElementFactory CreateStandardButtonTemplate(Color bgColor)
        {
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6)); // Chuẩn 6px

            var content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            border.AppendChild(content);
            return border;
        }

        private Button CreateQuickButton(string text, double radiusCm)
        {
            var button = new Button
            {
                Content = text,
                Width = 50,
                Height = 28,
                Background = new SolidColorBrush(Color.FromArgb(224, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                BorderThickness = new Thickness(1),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 5, 0),
                Cursor = Cursors.Hand,
                Focusable = false // ✅ QC_4.2_TOUCH_ACTIVATION: Vô hiệu hóa Focusable để không nuốt cú chạm đầu tiên trên IFP
            };
            Stylus.SetIsPressAndHoldEnabled(button, false);

            button.Click += (s, e) =>
            {
                _currentRadiusCm = radiusCm;
                UpdateCircle();
            };

            button.Template = new ControlTemplate(typeof(Button))
            {
                VisualTree = CreateQuickButtonTemplate()
            };

            // ✅ QC_4.2_TOUCH_ACTIVATION: Gắn trực tiếp Touch Pipeline cho nút kích thước nhanh
            QASmartTouch.Helpers.TouchActivationHelper.WireButton(button);

            return button;
        }

        private FrameworkElementFactory CreateQuickButtonTemplate()
        {
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));

            var content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            border.AppendChild(content);
            return border;
        }

        #endregion

        #region Update Methods

        private void UpdateCircle()
        {
            if (_mainDashboard == null) return;

            double radiusPx = _currentRadiusCm * CM_TO_PX;

            // Update circle
            if (_previewCircle != null)
            {
                _previewCircle.Width = radiusPx * 2;
                _previewCircle.Height = radiusPx * 2;
                Canvas.SetLeft(_previewCircle, _circleCenter.X - radiusPx);
                Canvas.SetTop(_previewCircle, _circleCenter.Y - radiusPx);
            }

            // Update diameter line
            if (_diameterLine != null)
            {
                _diameterLine.X1 = _circleCenter.X - radiusPx;
                _diameterLine.X2 = _circleCenter.X + radiusPx;
                _diameterLine.Y1 = _circleCenter.Y;
                _diameterLine.Y2 = _circleCenter.Y;
            }

            // Update arrows
            if (_leftArrow != null)
            {
                _leftArrow.Points = new PointCollection
                {
                    new Point(_circleCenter.X - radiusPx, _circleCenter.Y),
                    new Point(_circleCenter.X - radiusPx + 5, _circleCenter.Y - 3),
                    new Point(_circleCenter.X - radiusPx + 5, _circleCenter.Y + 3)
                };
            }

            if (_rightArrow != null)
            {
                _rightArrow.Points = new PointCollection
                {
                    new Point(_circleCenter.X + radiusPx, _circleCenter.Y),
                    new Point(_circleCenter.X + radiusPx - 5, _circleCenter.Y - 3),
                    new Point(_circleCenter.X + radiusPx - 5, _circleCenter.Y + 3)
                };
            }

            // Update label
            if (_sizeLabel != null)
            {
                _sizeLabel.Text = $"⌀ {_currentRadiusCm * 2:F1}cm";
            }

            if (_sizeLabelBorder != null)
            {
                Canvas.SetLeft(_sizeLabelBorder, _circleCenter.X - 30);
                Canvas.SetTop(_sizeLabelBorder, _circleCenter.Y - radiusPx - 30);
            }

            // Update center point position
            if (_centerPoint != null)
            {
                Canvas.SetLeft(_centerPoint, _circleCenter.X - 8);
                Canvas.SetTop(_centerPoint, _circleCenter.Y - 8);
            }

            // Update handles
            UpdateHandlePositions(radiusPx);

            // Update control panel position
            if (_controlPanel != null)
            {
                Canvas.SetLeft(_controlPanel, _circleCenter.X + radiusPx + 20);
                Canvas.SetTop(_controlPanel, _circleCenter.Y - 75);
            }

            // Update input
            if (_radiusInput != null)
            {
                _radiusInput.Text = _currentRadiusCm.ToString("F1");
            }
        }

        private void UpdateHandlePositions(double radiusPx)
        {
            if (_topHandle != null)
            {
                Canvas.SetLeft(_topHandle, _circleCenter.X - 8);
                Canvas.SetTop(_topHandle, _circleCenter.Y - radiusPx - 8);
            }

            if (_rightHandle != null)
            {
                Canvas.SetLeft(_rightHandle, _circleCenter.X + radiusPx - 8);
                Canvas.SetTop(_rightHandle, _circleCenter.Y - 8);
            }

            if (_bottomHandle != null)
            {
                Canvas.SetLeft(_bottomHandle, _circleCenter.X - 8);
                Canvas.SetTop(_bottomHandle, _circleCenter.Y + radiusPx - 8);
            }

            if (_leftHandle != null)
            {
                Canvas.SetLeft(_leftHandle, _circleCenter.X - radiusPx - 8);
                Canvas.SetTop(_leftHandle, _circleCenter.Y - 8);
            }
        }

        #endregion

        #region Center Point Drag Events

        private void CenterPoint_StartDrag(Point startPos)
        {
            if (_mainDashboard == null) return;
            _isDraggingCircle = true;
            _dragStartPoint = startPos;
        }

        private void CenterPoint_MoveDrag(Point currentPoint)
        {
            if (!_isDraggingCircle || _mainDashboard == null) return;

            double deltaX = currentPoint.X - _dragStartPoint.X;
            double deltaY = currentPoint.Y - _dragStartPoint.Y;

            _circleCenter = new Point(_circleCenter.X + deltaX, _circleCenter.Y + deltaY);
            _dragStartPoint = currentPoint;

            UpdateCircle();
        }

        private void CenterPoint_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_mainDashboard == null) return;

            CenterPoint_StartDrag(e.GetPosition(_mainDashboard.MainInteractiveBoard));
            _centerPoint?.CaptureMouse();
            e.Handled = true;
        }

        private void CenterPoint_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingCircle || _mainDashboard == null) return;

            CenterPoint_MoveDrag(e.GetPosition(_mainDashboard.MainInteractiveBoard));
            e.Handled = true;
        }

        private void CenterPoint_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingCircle = false;
            _centerPoint?.ReleaseMouseCapture();
            e.Handled = true;
        }

        // ✅ AUD-02: Named Touch/Stylus handlers cho CenterPoint (thay thế anonymous lambda)
        private void CenterPoint_TouchDown(object? sender, TouchEventArgs e)
        {
            if (_mainDashboard == null) return;
            CenterPoint_StartDrag(e.GetTouchPoint(_mainDashboard.MainInteractiveBoard).Position);
            _centerPoint?.CaptureTouch(e.TouchDevice);
            e.Handled = true;
        }

        private void CenterPoint_TouchMove(object? sender, TouchEventArgs e)
        {
            if (!_isDraggingCircle || _mainDashboard == null) return;
            CenterPoint_MoveDrag(e.GetTouchPoint(_mainDashboard.MainInteractiveBoard).Position);
            e.Handled = true;
        }

        private void CenterPoint_TouchUp(object? sender, TouchEventArgs e)
        {
            _isDraggingCircle = false;
            _centerPoint?.ReleaseTouchCapture(e.TouchDevice);
            e.Handled = true;
        }

        private void CenterPoint_StylusDown(object? sender, StylusDownEventArgs e)
        {
            if (_mainDashboard == null) return;
            CenterPoint_StartDrag(e.GetPosition(_mainDashboard.MainInteractiveBoard));
            _centerPoint?.CaptureStylus();
            e.Handled = true;
        }

        private void CenterPoint_StylusMove(object? sender, StylusEventArgs e)
        {
            if (!_isDraggingCircle || _mainDashboard == null) return;
            CenterPoint_MoveDrag(e.GetPosition(_mainDashboard.MainInteractiveBoard));
            e.Handled = true;
        }

        private void CenterPoint_StylusUp(object? sender, StylusEventArgs e)
        {
            _isDraggingCircle = false;
            _centerPoint?.ReleaseStylusCapture();
            e.Handled = true;
        }

        #endregion

        #region Handle Events

        private void Handle_StartDrag(Ellipse handle, Point startPos)
        {
            _isDraggingHandle = true;
            _activeHandle = handle;
        }

        private void Handle_MoveDrag(Point currentPoint)
        {
            if (!_isDraggingHandle || _activeHandle == null || _mainDashboard == null) return;

            double dx = currentPoint.X - _circleCenter.X;
            double dy = currentPoint.Y - _circleCenter.Y;
            double newRadiusPx = Math.Sqrt(dx * dx + dy * dy);

            double newRadiusCm = newRadiusPx / CM_TO_PX;
            newRadiusCm = Math.Clamp(newRadiusCm, 1, 20);

            _currentRadiusCm = newRadiusCm;
            UpdateCircle();
        }

        private void Handle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var handle = sender as Ellipse;
            if (handle != null && _mainDashboard != null)
            {
                Handle_StartDrag(handle, e.GetPosition(_mainDashboard.MainInteractiveBoard));
                handle.CaptureMouse();
                e.Handled = true;
            }
        }

        private void Handle_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingHandle || _activeHandle == null || _mainDashboard == null) return;

            Handle_MoveDrag(e.GetPosition(_mainDashboard.MainInteractiveBoard));
            e.Handled = true;
        }

        private void Handle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingHandle = false;
            _activeHandle?.ReleaseMouseCapture();
            _activeHandle = null;
            e.Handled = true;
        }

        // ✅ AUD-02: Named Touch/Stylus handlers cho Handles (thay thế anonymous lambda)
        private void Handle_TouchDown(object? sender, TouchEventArgs e)
        {
            var handle = sender as Ellipse;
            if (handle == null || _mainDashboard == null) return;
            Handle_StartDrag(handle, e.GetTouchPoint(_mainDashboard.MainInteractiveBoard).Position);
            handle.CaptureTouch(e.TouchDevice);
            e.Handled = true;
        }

        private void Handle_TouchMove(object? sender, TouchEventArgs e)
        {
            if (!_isDraggingHandle || _mainDashboard == null) return;
            Handle_MoveDrag(e.GetTouchPoint(_mainDashboard.MainInteractiveBoard).Position);
            e.Handled = true;
        }

        private void Handle_TouchUp(object? sender, TouchEventArgs e)
        {
            _isDraggingHandle = false;
            (sender as Ellipse)?.ReleaseTouchCapture(e.TouchDevice);
            _activeHandle = null;
            e.Handled = true;
        }

        private void Handle_StylusDown(object? sender, StylusDownEventArgs e)
        {
            var handle = sender as Ellipse;
            if (handle == null || _mainDashboard == null) return;
            Handle_StartDrag(handle, e.GetPosition(_mainDashboard.MainInteractiveBoard));
            handle.CaptureStylus();
            e.Handled = true;
        }

        private void Handle_StylusMove(object? sender, StylusEventArgs e)
        {
            if (!_isDraggingHandle || _mainDashboard == null) return;
            Handle_MoveDrag(e.GetPosition(_mainDashboard.MainInteractiveBoard));
            e.Handled = true;
        }

        private void Handle_StylusUp(object? sender, StylusEventArgs e)
        {
            _isDraggingHandle = false;
            (sender as Ellipse)?.ReleaseStylusCapture();
            _activeHandle = null;
            e.Handled = true;
        }

        #endregion

        #region Button Events

        private void btnDraw_Click(object sender, RoutedEventArgs e)
        {
            if (_mainDashboard == null) return;

            try
            {
                double radiusPx = _currentRadiusCm * CM_TO_PX;

                // Create a container for circle + center point (IsHitTestVisible = false để cố định 100% không bị xô lệch)
                var container = new Canvas
                {
                    Width = radiusPx * 2,
                    Height = radiusPx * 2,
                    IsHitTestVisible = false // ✅ FIX: Standard pedagogical shape - completely static & write-through
                };

                // Create the circle
                var circle = new Ellipse
                {
                    Width = radiusPx * 2,
                    Height = radiusPx * 2,
                    Stroke = new SolidColorBrush(_strokeColor),
                    StrokeThickness = _strokeThickness,
                    Fill = null, // ✅ FIX: Null fill so pen/line drawing passes directly to board
                    IsHitTestVisible = false,
                    SnapsToDevicePixels = true
                };
                Canvas.SetLeft(circle, 0);
                Canvas.SetTop(circle, 0);
                container.Children.Add(circle);

                // Create center point (cross mark)
                var centerMark = CreateCenterMark(radiusPx);
                centerMark.IsHitTestVisible = false;
                container.Children.Add(centerMark);

                // Position the container
                Canvas.SetLeft(container, _circleCenter.X - radiusPx);
                Canvas.SetTop(container, _circleCenter.Y - radiusPx);

                _mainDashboard.MainInteractiveBoard.Children.Add(container);

                // ✅ Record action for Undo/Redo support
                try
                {
                    _mainDashboard.RecordToolDrawAction(container, $"Circle: radius={_currentRadiusCm:F1}cm");
                }
                catch { }

                ShowTemporaryMessage($"✓ Đã vẽ hình tròn bán kính {_currentRadiusCm:F1}cm");
                System.Media.SystemSounds.Asterisk.Play();

                // ✅ FIX SƯ PHẠM: Tự động dọn dẹp và đóng công cụ Compa sau khi vẽ xong
                RemoveAllFromCanvas();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi vẽ hình tròn: {ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Creates a center mark (filled dot) at the center of the circle
        /// </summary>
        private Canvas CreateCenterMark(double radiusPx)
        {
            var centerCanvas = new Canvas();

            double centerX = radiusPx;
            double centerY = radiusPx;
            double dotRadius = Math.Max(3.5, _strokeThickness * 1.5); // Dot size scales with stroke

            // Filled dot (chấm tròn đặc) thay cho dấu +
            var centerDot = new Ellipse
            {
                Width  = dotRadius * 2,
                Height = dotRadius * 2,
                Fill   = new SolidColorBrush(_strokeColor),
                SnapsToDevicePixels = true
            };
            Canvas.SetLeft(centerDot, centerX - dotRadius);
            Canvas.SetTop(centerDot,  centerY - dotRadius);
            centerCanvas.Children.Add(centerDot);

            return centerCanvas;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            RemoveAllFromCanvas();
            this.Close();
            try
            {
                _mainDashboard?.Activate();
                _mainDashboard?.Focus();
            }
            catch { }
        }

        /// <summary>
        /// ✅ AUD-02 FIX: Đảm bảo cleanup khi đóng cửa sổ bằng Alt+F4 hoặc code Close()
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            RemoveAllFromCanvas();
            base.OnClosed(e);
            try
            {
                _mainDashboard?.Activate();
                _mainDashboard?.Focus();
            }
            catch { }
        }



        #endregion

        #region Radius Input Events

        private void RadiusInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (double.TryParse(_radiusInput?.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double radiusCm))
                {
                    radiusCm = Math.Clamp(radiusCm, 1, 20);
                    _currentRadiusCm = radiusCm;
                    UpdateCircle();
                }
                e.Handled = true;
            }
        }

        #endregion

        #region Helper Methods

        private void RemoveAllFromCanvas()
        {
            if (_mainDashboard == null) return;

            // ✅ AUD-02 FIX: Hủy đăng ký TOÀN BỘ Event Handlers để tránh rò rỉ bộ nhớ
            if (_centerPoint != null)
            {
                _centerPoint.MouseLeftButtonDown -= CenterPoint_MouseDown;
                _centerPoint.MouseMove -= CenterPoint_MouseMove;
                _centerPoint.MouseLeftButtonUp -= CenterPoint_MouseUp;
                _centerPoint.TouchDown -= CenterPoint_TouchDown;
                _centerPoint.TouchMove -= CenterPoint_TouchMove;
                _centerPoint.TouchUp -= CenterPoint_TouchUp;
                _centerPoint.StylusDown -= CenterPoint_StylusDown;
                _centerPoint.StylusMove -= CenterPoint_StylusMove;
                _centerPoint.StylusUp -= CenterPoint_StylusUp;
            }

            // ✅ AUD-02 FIX: Hủy đăng ký Event Handlers trên tất cả 4 Handles
            foreach (var handle in new[] { _topHandle, _rightHandle, _bottomHandle, _leftHandle })
            {
                if (handle == null) continue;
                handle.MouseLeftButtonDown -= Handle_MouseDown;
                handle.MouseMove -= Handle_MouseMove;
                handle.MouseLeftButtonUp -= Handle_MouseUp;
                handle.TouchDown -= Handle_TouchDown;
                handle.TouchMove -= Handle_TouchMove;
                handle.TouchUp -= Handle_TouchUp;
                handle.StylusDown -= Handle_StylusDown;
                handle.StylusMove -= Handle_StylusMove;
                handle.StylusUp -= Handle_StylusUp;
            }

            // Xóa toàn bộ phần tử khỏi Canvas
            if (_previewCircle != null) _mainDashboard.MainInteractiveBoard.Children.Remove(_previewCircle);
            if (_centerPoint != null) _mainDashboard.MainInteractiveBoard.Children.Remove(_centerPoint);
            if (_diameterLine != null) _mainDashboard.MainInteractiveBoard.Children.Remove(_diameterLine);
            if (_leftArrow != null) _mainDashboard.MainInteractiveBoard.Children.Remove(_leftArrow);
            if (_rightArrow != null) _mainDashboard.MainInteractiveBoard.Children.Remove(_rightArrow);
            if (_sizeLabelBorder != null) _mainDashboard.MainInteractiveBoard.Children.Remove(_sizeLabelBorder);
            if (_topHandle != null) _mainDashboard.MainInteractiveBoard.Children.Remove(_topHandle);
            if (_rightHandle != null) _mainDashboard.MainInteractiveBoard.Children.Remove(_rightHandle);
            if (_bottomHandle != null) _mainDashboard.MainInteractiveBoard.Children.Remove(_bottomHandle);
            if (_leftHandle != null) _mainDashboard.MainInteractiveBoard.Children.Remove(_leftHandle);
            if (_controlPanel != null) _mainDashboard.MainInteractiveBoard.Children.Remove(_controlPanel);

            // ✅ FIX: Null hóa tham chiếu để GC có thể thu hồi bộ nhớ
            _previewCircle = null;
            _centerPoint = null;
            _diameterLine = null;
            _leftArrow = null;
            _rightArrow = null;
            _sizeLabelBorder = null;
            _sizeLabel = null;
            _topHandle = null;
            _rightHandle = null;
            _bottomHandle = null;
            _leftHandle = null;
            _controlPanel = null;
            _radiusInput = null;
            _activeHandle = null;        // ✅ AUD-03 FIX
            _mainDashboard = null;       // ✅ AUD-03 FIX: Giải phóng tham chiếu tới cửa sổ chính
        }

 

        private void ShowTemporaryMessage(string message)
        {
            if (_mainDashboard == null) return;

            // ✅ FIX TIMER-NULLREF: Capture local reference để timer lambda
            // không bị NullReferenceException khi _mainDashboard đã bị null hóa
            // bởi RemoveAllFromCanvas() trước khi timer tick (sau 2 giây).
            var canvas = _mainDashboard.MainInteractiveBoard;

            Border messageBox = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(230, 76, 175, 80)),
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

            canvas.Children.Add(messageBox);

            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            timer.Tick += (s, e) =>
            {
                canvas.Children.Remove(messageBox);
                timer.Stop();
            };
            timer.Start();
        }

        #endregion

        #region Move Button Events

        // ✅ FIX: Toàn bộ logic di chuyển nút ✥ được chuẩn hóa theo Form2_15_RulerTool
        // Sử dụng tọa độ MÀN HÌNH TUYỆT ĐỐI (PointToScreen) thay vì tọa độ tương đối Canvas.
        // LÝ DO: Khi _controlPanel (chứa btnMove) di chuyển trên Canvas, tọa độ tương đối
        //   của Mouse event bị xê dịch cùng lúc với panel → tạo delta ngược dấu ở frame sau
        //   → Coordinate Feedback Loop → hiện tượng "nháy nháy" tại chỗ.
        // Tọa độ PointToScreen() luôn ổn định vì tham chiếu hệ tọa độ màn hình vật lý,
        //   không bị ảnh hưởng bởi việc Canvas.SetLeft/SetTop thay đổi vị trí panel.

        private void btnMove_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_mainDashboard == null || _controlPanel == null) return;

            _isDraggingPanel = true;
            // ✅ Lấy tọa độ màn hình tuyệt đối (Screen Coordinates) - giống Form2_15_RulerTool
            _panelDragStart = _mainDashboard.PointToScreen(e.GetPosition(_mainDashboard));

            if (sender is UIElement el)
                el.CaptureMouse();

            e.Handled = true;
        }

        private void btnMove_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingPanel || _mainDashboard == null || _controlPanel == null) return;

            // ✅ Tính delta bằng tọa độ tuyệt đối - loại bỏ hoàn toàn Coordinate Feedback Loop
            var currentScreenPoint = _mainDashboard.PointToScreen(e.GetPosition(_mainDashboard));

            double deltaX = currentScreenPoint.X - _panelDragStart.X;
            double deltaY = currentScreenPoint.Y - _panelDragStart.Y;

            _circleCenter = new Point(_circleCenter.X + deltaX, _circleCenter.Y + deltaY);
            _panelDragStart = currentScreenPoint;

            UpdateCircle();
            e.Handled = true;
        }

        private void btnMove_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingPanel)
            {
                _isDraggingPanel = false;

                if (sender is UIElement el)
                    el.ReleaseMouseCapture();

                e.Handled = true;
            }
        }

        // ============ TOUCH MOVE HANDLERS (QC_4.2_TOUCH_PIPELINE) ============
        private int? _panelTouchId = null;

        private void btnMove_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            if (_mainDashboard == null || _controlPanel == null) return;

            if (sender is UIElement el)
            {
                _panelTouchId = e.TouchDevice.Id;
                _isDraggingPanel = true;
                _panelDragStart = _mainDashboard.PointToScreen(e.GetTouchPoint(_mainDashboard).Position);
                el.CaptureTouch(e.TouchDevice);
                e.Handled = true;
            }
        }

        private void btnMove_TouchMove(object sender, TouchEventArgs e)
        {
            if (!_isDraggingPanel || _mainDashboard == null || _controlPanel == null) return;
            if (_panelTouchId != e.TouchDevice.Id) return;

            var currentScreenPoint = _mainDashboard.PointToScreen(e.GetTouchPoint(_mainDashboard).Position);

            double deltaX = currentScreenPoint.X - _panelDragStart.X;
            double deltaY = currentScreenPoint.Y - _panelDragStart.Y;

            _circleCenter = new Point(_circleCenter.X + deltaX, _circleCenter.Y + deltaY);
            _panelDragStart = currentScreenPoint;

            UpdateCircle();
            e.Handled = true;
        }

        private void btnMove_TouchUp(object sender, TouchEventArgs e)
        {
            if (_panelTouchId == e.TouchDevice.Id)
            {
                _isDraggingPanel = false;
                _panelTouchId = null;

                if (sender is UIElement el && e.TouchDevice.Captured == el)
                    el.ReleaseTouchCapture(e.TouchDevice);

                e.Handled = true;
            }
        }

        private void btnMove_LostTouchCapture(object sender, TouchEventArgs e)
        {
            if (_panelTouchId == e.TouchDevice.Id)
            {
                _isDraggingPanel = false;
                _panelTouchId = null;
            }
        }

        #endregion

        #region Public Methods

        public void SetMainDashboard(Form2_MainDashboard mainDashboard)
        {
            _mainDashboard = mainDashboard;
        }

        #endregion
    }
}
