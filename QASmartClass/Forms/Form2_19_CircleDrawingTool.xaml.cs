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

        // Circle center position (Needle tip A)
        private Point _circleCenter;

        // Current angle of the Compa pencil tip around _circleCenter (in radians, 0 = right)
        private double _currentAngleRad = 0.0;

        // Live swing-to-draw state
        private bool _isSwingingPencil = false;
        private int? _swingTouchId = null;
        private double _sweepStartAngleRad = 0.0;
        private double _lastSwingAngleRad = 0.0;
        private double _sweepAccumulatedRad = 0.0;
        private Path? _liveSweepPath;

        // Preview circle and guide elements
        private Ellipse? _previewCircle;
        private Ellipse? _centerPoint;
        private Line? _diameterLine;
        private Polygon? _leftArrow;
        private Polygon? _rightArrow;
        private Border? _sizeLabelBorder;
        private TextBlock? _sizeLabel;

        // Realistic 2.5D Compa visual elements
        private Canvas? _compassCanvas;
        private Line? _bowBarLine;
        private Line? _bowThreadLine;
        private Polygon? _bowWheel;
        private Polygon? _needlePin;
        private Polygon? _needleChuck;
        private Polygon? _needleLegBody;
        private Polygon? _pencilLegBody;
        private Polygon? _pencilLeadTip;
        private Polygon? _pencilWoodCone;
        private Polygon? _pencilBarrel;
        private Polygon? _pencilColorStripe;
        private Polygon? _pencilFerrule;
        private Polygon? _pencilEraser;
        private Polygon? _pencilClampCollar;
        private Ellipse? _pencilClampScrew;
        private Polygon? _topGripPeg;
        private Ellipse? _topHingeOuter;
        private Ellipse? _topHingeInner;
        private Border? _pencilTipBadge;

        // Interactive hit targets on the Compa body
        private Polygon? _hingeHitPolygon;
        private Polygon? _pencilHitPolygon;
        private Ellipse? _bowWheelHitArea;

        // Guide Circle Handles (for adjusting radius)
        private Ellipse? _topHandle;
        private Ellipse? _rightHandle;
        private Ellipse? _bottomHandle;
        private Ellipse? _leftHandle;

        // Control panel
        private Border? _controlPanel;
        private TextBox? _radiusInput;

        // Radius drag states (Delta-based + Touch-isolated)
        private bool _isDraggingHandle = false;
        private int? _resizeTouchId = null;
        private UIElement? _activeHandle = null;
        private double _resizeStartRadiusCm = 5.0;
        private double _resizeStartDistPx = 0.0;

        // Move Compa drag states (Touch-isolated)
        private bool _isDraggingCircle = false;
        private int? _moveTouchId = null;
        private Point _dragStartPoint;
        private UIElement? _activeMoveElement = null;

        // Control panel drag state
        private bool _isDraggingPanel = false;
        private Point _panelDragStart;

        // Stroke settings & cached brush
        private Color _strokeColor = Colors.Black;
        private double _strokeThickness = 2.0;
        private SolidColorBrush _activeStrokeBrush = new SolidColorBrush(Colors.Black);

        #endregion

        #region Constructor

        public Form2_19_CircleDrawingTool()
        {
            InitializeComponent();
            QASmartTouch.Helpers.TouchActivationHelper.Apply(this);
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_mainDashboard == null) return;

            this.ShowInTaskbar = false;

            // Set initial circle center (middle of screen)
            _circleCenter = new Point(
                _mainDashboard.MainInteractiveBoard.ActualWidth / 2,
                _mainDashboard.MainInteractiveBoard.ActualHeight / 2 + 30
            );

            // Create all UI on canvas
            CreateUIOnCanvas();

            // Collapse host window after embedding UI onto MainInteractiveBoard
            this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                this.Visibility = Visibility.Collapsed;
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

            SyncPenSettingsFromDashboard();

            // Create guide circle + realistic Compa body
            CreatePreviewCircle();

            // Create control panel below/next to Compa
            CreateControlPanel();

            // Initial geometry layout
            UpdateCircle();
        }

        private void SyncPenSettingsFromDashboard()
        {
            if (_mainDashboard != null)
            {
                _strokeColor = _mainDashboard.CurrentPenColor;
                _strokeThickness = Math.Max(2.0, _mainDashboard.CurrentPenSize);
                _activeStrokeBrush = new SolidColorBrush(_strokeColor);
            }
        }

        private void CreatePreviewCircle()
        {
            if (_mainDashboard == null) return;

            double radiusPx = _currentRadiusCm * CM_TO_PX;

            // 1. Dashed guide circle
            _previewCircle = new Ellipse
            {
                Width = radiusPx * 2,
                Height = radiusPx * 2,
                Stroke = new SolidColorBrush(Color.FromArgb(150, 59, 130, 246)),
                StrokeThickness = 2,
                Fill = Brushes.Transparent,
                StrokeDashArray = new DoubleCollection { 5, 4 },
                IsHitTestVisible = false,
                Tag = "DragHandle"
            };
            Panel.SetZIndex(_previewCircle, 10000);
            _mainDashboard.MainInteractiveBoard.Children.Add(_previewCircle);

            // 2. Live ink sweep path (visible while user swings the Compa pencil)
            _liveSweepPath = new Path
            {
                Stroke = _activeStrokeBrush,
                StrokeThickness = _strokeThickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Fill = Brushes.Transparent,
                IsHitTestVisible = false,
                Tag = "DragHandle"
            };
            Panel.SetZIndex(_liveSweepPath, 10000);
            _mainDashboard.MainInteractiveBoard.Children.Add(_liveSweepPath);

            // 3. Radius vector line (from center O to pencil tip B)
            _diameterLine = new Line
            {
                Stroke = new SolidColorBrush(Color.FromArgb(190, 16, 185, 129)),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                IsHitTestVisible = false,
                Tag = "DragHandle"
            };
            Panel.SetZIndex(_diameterLine, 10000);
            _mainDashboard.MainInteractiveBoard.Children.Add(_diameterLine);

            // Arrows (kept minimal for clean look)
            _leftArrow = new Polygon
            {
                Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                IsHitTestVisible = false,
                Tag = "DragHandle"
            };
            Panel.SetZIndex(_leftArrow, 10000);
            _mainDashboard.MainInteractiveBoard.Children.Add(_leftArrow);

            _rightArrow = new Polygon
            {
                Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                IsHitTestVisible = false,
                Tag = "DragHandle"
            };
            Panel.SetZIndex(_rightArrow, 10000);
            _mainDashboard.MainInteractiveBoard.Children.Add(_rightArrow);

            // 4. Create realistic 2.5D Compa visual canvas
            CreateCompassVisualElements();

            // 5. Center needle target point (draggable)
            _centerPoint = new Ellipse
            {
                Width = 22,
                Height = 22,
                Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                Stroke = Brushes.White,
                StrokeThickness = 2.5,
                Cursor = Cursors.SizeAll,
                ToolTip = "Tâm Compa (Kéo để di chuyển Compa)",
                Tag = "DragHandle"
            };
            Panel.SetZIndex(_centerPoint, 10002);
            WireMoveElementEvents(_centerPoint);
            _mainDashboard.MainInteractiveBoard.Children.Add(_centerPoint);

            // 6. Live Radius & Angle Badge
            _sizeLabel = new TextBlock
            {
                Text = $"R = {_currentRadiusCm:F1}cm (⌀ {_currentRadiusCm * 2:F1}cm)",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59))
            };

            _sizeLabelBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(240, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 3, 8, 3),
                Child = _sizeLabel,
                IsHitTestVisible = false,
                Tag = "DragHandle"
            };
            Panel.SetZIndex(_sizeLabelBorder, 10002);
            _mainDashboard.MainInteractiveBoard.Children.Add(_sizeLabelBorder);

            // 7. Radius adjustment handles on the guide circle
            CreateHandles(radiusPx);
        }

        private static Polygon CreatePreallocatedPolygon(int vertexCount, Brush? fill, Brush? stroke, double strokeThickness, bool isHitTestVisible = false)
        {
            var pts = new PointCollection(vertexCount);
            for (int i = 0; i < vertexCount; i++)
            {
                pts.Add(new Point());
            }
            return new Polygon
            {
                Points = pts,
                Fill = fill,
                Stroke = stroke,
                StrokeThickness = strokeThickness,
                IsHitTestVisible = isHitTestVisible,
                Tag = "DragHandle"
            };
        }

        private static void SetQuadPoints(Polygon? poly, Point p0, Point p1, Point p2, Point p3)
        {
            if (poly?.Points == null || poly.Points.Count < 4) return;
            poly.Points[0] = p0;
            poly.Points[1] = p1;
            poly.Points[2] = p2;
            poly.Points[3] = p3;
        }

        private static void SetTrianglePoints(Polygon? poly, Point p0, Point p1, Point p2)
        {
            if (poly?.Points == null || poly.Points.Count < 3) return;
            poly.Points[0] = p0;
            poly.Points[1] = p1;
            poly.Points[2] = p2;
        }

        private void CreateCompassVisualElements()
        {
            if (_mainDashboard == null) return;

            _compassCanvas = new Canvas
            {
                IsHitTestVisible = true,
                Tag = "DragHandle"
            };
            Panel.SetZIndex(_compassCanvas, 10001);

            var steelBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0)
            };
            steelBrush.GradientStops.Add(new GradientStop(Color.FromRgb(248, 250, 252), 0.0));
            steelBrush.GradientStops.Add(new GradientStop(Color.FromRgb(203, 213, 225), 0.55));
            steelBrush.GradientStops.Add(new GradientStop(Color.FromRgb(100, 116, 139), 1.0));

            var darkMetalBrush = new SolidColorBrush(Color.FromRgb(30, 41, 59));
            var steelStroke = new SolidColorBrush(Color.FromRgb(51, 65, 85));

            // 1. Middle bow bar & thread
            _bowBarLine = new Line
            {
                Stroke = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                StrokeThickness = 5,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                IsHitTestVisible = false,
                Tag = "DragHandle"
            };
            _compassCanvas.Children.Add(_bowBarLine);

            _bowThreadLine = new Line
            {
                Stroke = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                StrokeThickness = 3,
                StrokeDashArray = new DoubleCollection { 1.5, 1.5 },
                IsHitTestVisible = false,
                Tag = "DragHandle"
            };
            _compassCanvas.Children.Add(_bowThreadLine);

            _bowWheel = CreatePreallocatedPolygon(
                4,
                new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                new SolidColorBrush(Color.FromRgb(180, 83, 9)),
                1.5);
            _compassCanvas.Children.Add(_bowWheel);

            // 2. Needle Leg (Left/Center Leg)
            _needlePin = CreatePreallocatedPolygon(
                3,
                new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                steelStroke,
                1.2);
            _compassCanvas.Children.Add(_needlePin);

            _needleChuck = CreatePreallocatedPolygon(
                4,
                darkMetalBrush,
                new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                1.0);
            _compassCanvas.Children.Add(_needleChuck);

            _needleLegBody = CreatePreallocatedPolygon(4, steelBrush, steelStroke, 1.5);
            _compassCanvas.Children.Add(_needleLegBody);

            // 3. Pencil Leg (Right/Outer Leg)
            _pencilLegBody = CreatePreallocatedPolygon(4, steelBrush, steelStroke, 1.5);
            _compassCanvas.Children.Add(_pencilLegBody);

            // 4. Realistic Pencil (Lead tip -> Wood cone -> Barrel -> Color stripe -> Ferrule -> Eraser)
            _pencilLeadTip = CreatePreallocatedPolygon(3, _activeStrokeBrush, darkMetalBrush, 0.8);
            _compassCanvas.Children.Add(_pencilLeadTip);

            _pencilWoodCone = CreatePreallocatedPolygon(
                4,
                new SolidColorBrush(Color.FromRgb(253, 230, 138)),
                new SolidColorBrush(Color.FromRgb(180, 83, 9)),
                1.2);
            _compassCanvas.Children.Add(_pencilWoodCone);

            _pencilBarrel = CreatePreallocatedPolygon(
                4,
                new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                new SolidColorBrush(Color.FromRgb(180, 83, 9)),
                1.2);
            _compassCanvas.Children.Add(_pencilBarrel);

            _pencilColorStripe = CreatePreallocatedPolygon(4, _activeStrokeBrush, null, 0.0);
            _compassCanvas.Children.Add(_pencilColorStripe);

            _pencilFerrule = CreatePreallocatedPolygon(
                4,
                new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                1.2);
            _compassCanvas.Children.Add(_pencilFerrule);

            _pencilEraser = CreatePreallocatedPolygon(
                4,
                new SolidColorBrush(Color.FromRgb(251, 113, 133)),
                new SolidColorBrush(Color.FromRgb(190, 18, 60)),
                1.2);
            _compassCanvas.Children.Add(_pencilEraser);

            // 5. Metal Pencil Clamp Collar & Screw
            _pencilClampCollar = CreatePreallocatedPolygon(
                4,
                darkMetalBrush,
                new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                1.3);
            _compassCanvas.Children.Add(_pencilClampCollar);

            _pencilClampScrew = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                Stroke = Brushes.White,
                StrokeThickness = 1.2,
                IsHitTestVisible = false,
                Tag = "DragHandle"
            };
            _compassCanvas.Children.Add(_pencilClampScrew);

            // 6. Top Grip Peg & Pivot Hinge Yoke
            _topGripPeg = CreatePreallocatedPolygon(
                4,
                darkMetalBrush,
                new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                1.5);
            _compassCanvas.Children.Add(_topGripPeg);

            _topHingeOuter = new Ellipse
            {
                Width = 28,
                Height = 28,
                Fill = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                Stroke = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                StrokeThickness = 2.2,
                IsHitTestVisible = false,
                Tag = "DragHandle"
            };
            _compassCanvas.Children.Add(_topHingeOuter);

            _topHingeInner = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                Stroke = Brushes.White,
                StrokeThickness = 1.5,
                IsHitTestVisible = false,
                Tag = "DragHandle"
            };
            _compassCanvas.Children.Add(_topHingeInner);

            // 7. Visual Swing Hint Badge near Pencil Tip ("↻")
            _pencilTipBadge = new Border
            {
                Width = 26,
                Height = 26,
                CornerRadius = new CornerRadius(13),
                Background = new SolidColorBrush(Color.FromArgb(235, 37, 99, 235)),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(2),
                Child = new TextBlock
                {
                    Text = "↻",
                    Foreground = Brushes.White,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, -1, 0, 0)
                },
                IsHitTestVisible = false,
                Tag = "DragHandle"
            };
            _compassCanvas.Children.Add(_pencilTipBadge);

            // 8. Interactive Hit Zones on the Compa
            // 8a. Top Hinge & Needle Leg Hit Zone -> Move Compa
            _hingeHitPolygon = CreatePreallocatedPolygon(6, Brushes.Transparent, null, 0.0, isHitTestVisible: true);
            _hingeHitPolygon.Cursor = Cursors.SizeAll;
            _hingeHitPolygon.ToolTip = "Núm cầm đỉnh Compa (Kéo để di chuyển Compa)";
            WireMoveElementEvents(_hingeHitPolygon);
            _compassCanvas.Children.Add(_hingeHitPolygon);

            // 8b. Bow Wheel Hit Zone -> Adjust Radius
            _bowWheelHitArea = new Ellipse
            {
                Width = 40,
                Height = 40,
                Fill = Brushes.Transparent,
                Cursor = Cursors.SizeWE,
                ToolTip = "Bánh xe chỉnh độ mở Compa (Kéo để thay đổi bán kính)",
                Tag = "DragHandle"
            };
            WireRadiusHandleEvents(_bowWheelHitArea);
            _compassCanvas.Children.Add(_bowWheelHitArea);

            // 8c. Pencil & Pencil Tip Hit Zone -> Swing Compa to Draw Circle/Arc
            _pencilHitPolygon = CreatePreallocatedPolygon(4, Brushes.Transparent, null, 0.0, isHitTestVisible: true);
            _pencilHitPolygon.Cursor = Cursors.Hand;
            _pencilHitPolygon.ToolTip = "Cầm bút chì xoay quanh tâm để vẽ đường tròn / cung tròn";
            WirePencilSwingEvents(_pencilHitPolygon);
            _compassCanvas.Children.Add(_pencilHitPolygon);

            _mainDashboard.MainInteractiveBoard.Children.Add(_compassCanvas);
        }

        private void CreateHandles(double radiusPx)
        {
            if (_mainDashboard == null) return;

            _topHandle = CreateHandle();
            Panel.SetZIndex(_topHandle, 10002);
            _mainDashboard.MainInteractiveBoard.Children.Add(_topHandle);

            _rightHandle = CreateHandle();
            Panel.SetZIndex(_rightHandle, 10002);
            _mainDashboard.MainInteractiveBoard.Children.Add(_rightHandle);

            _bottomHandle = CreateHandle();
            Panel.SetZIndex(_bottomHandle, 10002);
            _mainDashboard.MainInteractiveBoard.Children.Add(_bottomHandle);

            _leftHandle = CreateHandle();
            Panel.SetZIndex(_leftHandle, 10002);
            _mainDashboard.MainInteractiveBoard.Children.Add(_leftHandle);
        }

        private Ellipse CreateHandle()
        {
            var handle = new Ellipse
            {
                Width = 14,
                Height = 14,
                Fill = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                Stroke = Brushes.White,
                StrokeThickness = 2.0,
                Cursor = Cursors.Hand,
                ToolTip = "Kéo để mở rộng / thu hẹp bán kính Compa",
                Tag = "DragHandle"
            };

            WireRadiusHandleEvents(handle);
            return handle;
        }

        private void WireMoveElementEvents(UIElement element)
        {
            Stylus.SetIsPressAndHoldEnabled(element, false);
            Stylus.SetIsFlicksEnabled(element, false);
            element.MouseLeftButtonDown += CenterPoint_MouseDown;
            element.MouseMove += CenterPoint_MouseMove;
            element.MouseLeftButtonUp += CenterPoint_MouseUp;
            element.PreviewTouchDown += CenterPoint_TouchDown;
            element.TouchMove += CenterPoint_TouchMove;
            element.TouchUp += CenterPoint_TouchUp;
            element.LostTouchCapture += CenterPoint_LostTouchCapture;
        }

        private void UnwireMoveElementEvents(UIElement? element)
        {
            if (element == null) return;
            element.MouseLeftButtonDown -= CenterPoint_MouseDown;
            element.MouseMove -= CenterPoint_MouseMove;
            element.MouseLeftButtonUp -= CenterPoint_MouseUp;
            element.PreviewTouchDown -= CenterPoint_TouchDown;
            element.TouchMove -= CenterPoint_TouchMove;
            element.TouchUp -= CenterPoint_TouchUp;
            element.LostTouchCapture -= CenterPoint_LostTouchCapture;
        }

        private void WireRadiusHandleEvents(UIElement handle)
        {
            Stylus.SetIsPressAndHoldEnabled(handle, false);
            Stylus.SetIsFlicksEnabled(handle, false);
            handle.MouseLeftButtonDown += Handle_MouseDown;
            handle.MouseMove += Handle_MouseMove;
            handle.MouseLeftButtonUp += Handle_MouseUp;
            handle.PreviewTouchDown += Handle_TouchDown;
            handle.TouchMove += Handle_TouchMove;
            handle.TouchUp += Handle_TouchUp;
            handle.LostTouchCapture += Handle_LostTouchCapture;
        }

        private void UnwireRadiusHandleEvents(UIElement? handle)
        {
            if (handle == null) return;
            handle.MouseLeftButtonDown -= Handle_MouseDown;
            handle.MouseMove -= Handle_MouseMove;
            handle.MouseLeftButtonUp -= Handle_MouseUp;
            handle.PreviewTouchDown -= Handle_TouchDown;
            handle.TouchMove -= Handle_TouchMove;
            handle.TouchUp -= Handle_TouchUp;
            handle.LostTouchCapture -= Handle_LostTouchCapture;
        }

        private void WirePencilSwingEvents(UIElement element)
        {
            Stylus.SetIsPressAndHoldEnabled(element, false);
            Stylus.SetIsFlicksEnabled(element, false);
            element.MouseLeftButtonDown += Pencil_MouseDown;
            element.MouseMove += Pencil_MouseMove;
            element.MouseLeftButtonUp += Pencil_MouseUp;
            element.PreviewTouchDown += Pencil_TouchDown;
            element.TouchMove += Pencil_TouchMove;
            element.TouchUp += Pencil_TouchUp;
            element.LostTouchCapture += Pencil_LostTouchCapture;
        }

        private void UnwirePencilSwingEvents(UIElement? element)
        {
            if (element == null) return;
            element.MouseLeftButtonDown -= Pencil_MouseDown;
            element.MouseMove -= Pencil_MouseMove;
            element.MouseLeftButtonUp -= Pencil_MouseUp;
            element.PreviewTouchDown -= Pencil_TouchDown;
            element.TouchMove -= Pencil_TouchMove;
            element.TouchUp -= Pencil_TouchUp;
            element.LostTouchCapture -= Pencil_LostTouchCapture;
        }

        private void CreateControlPanel()
        {
            if (_mainDashboard == null) return;

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

            controlRow.Children.Add(new TextBlock
            {
                Text = "Bán kính:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0),
                FontSize = 12
            });

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

            controlRow.Children.Add(new TextBlock
            {
                Text = "cm",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                FontSize = 12,
                FontWeight = FontWeights.Bold
            });

            // 1-Tap Draw Full Circle button
            var btnDraw = CreateStandardButton("✓ Vẽ", Color.FromRgb(16, 185, 129), 54);
            btnDraw.ToolTip = "Vẽ nhanh đường tròn 360° (Hoặc cầm trực tiếp cây bút chì trên Compa xoay để vẽ)";
            btnDraw.Click += btnDraw_Click;
            btnDraw.Margin = new Thickness(0, 0, 5, 0);
            QASmartTouch.Helpers.TouchActivationHelper.WireButton(btnDraw);
            controlRow.Children.Add(btnDraw);

            // Move button
            var btnMove = CreateStandardButton("✥", Color.FromRgb(102, 126, 234), 32);
            btnMove.ToolTip = "Kéo để di chuyển Compa";
            btnMove.FontSize = 14;
            btnMove.FontWeight = FontWeights.Bold;
            btnMove.Cursor = Cursors.SizeAll;
            btnMove.Margin = new Thickness(0, 0, 5, 0);
            btnMove.Tag = "DragHandle";

            Stylus.SetIsPressAndHoldEnabled(btnMove, false);
            Stylus.SetIsFlicksEnabled(btnMove, false);
            btnMove.PreviewMouseDown += btnMove_PreviewMouseDown;
            btnMove.PreviewMouseMove += btnMove_PreviewMouseMove;
            btnMove.PreviewMouseUp += btnMove_PreviewMouseUp;
            btnMove.PreviewTouchDown += btnMove_PreviewTouchDown;
            btnMove.TouchMove += btnMove_TouchMove;
            btnMove.TouchUp += btnMove_TouchUp;
            btnMove.LostTouchCapture += btnMove_LostTouchCapture;

            controlRow.Children.Add(btnMove);

            // Close button
            var btnClose = CreateStandardButton("✕", Color.FromRgb(239, 68, 68), 32);
            btnClose.Click += btnClose_Click;
            btnClose.FontSize = 13;
            btnClose.FontWeight = FontWeights.Bold;
            QASmartTouch.Helpers.TouchActivationHelper.WireButton(btnClose);
            controlRow.Children.Add(btnClose);

            mainStack.Children.Add(controlRow);

            // Row 2: Quick radius buttons
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

            _controlPanel = new Border
            {
                Child = mainStack,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Tag = "DragHandle"
            };

            _controlPanel.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Color.FromRgb(148, 163, 184),
                Direction = 270,
                ShadowDepth = 3,
                BlurRadius = 14,
                Opacity = 0.28
            };

            Panel.SetZIndex(_controlPanel, 10003);
            _mainDashboard.MainInteractiveBoard.Children.Add(_controlPanel);

            QASmartTouch.Helpers.TouchActivationHelper.WireAllInteractiveControls(_controlPanel, btnMove);
        }

        private Button CreateStandardButton(string content, Color bgColor, double width)
        {
            var button = new Button
            {
                Content = content,
                Width = width,
                Height = 32,
                Background = new SolidColorBrush(bgColor),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                Focusable = false
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
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));

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
                Background = new SolidColorBrush(Color.FromArgb(240, 248, 250, 252)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                BorderThickness = new Thickness(1),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 5, 0),
                Cursor = Cursors.Hand,
                Focusable = false
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

        #region Update & 2.5D Compa Kinematics

        private void UpdateCircle()
        {
            if (_mainDashboard == null) return;

            double radiusPx = _currentRadiusCm * CM_TO_PX;
            Point centerA = _circleCenter;
            Point pencilTipB = new Point(
                centerA.X + radiusPx * Math.Cos(_currentAngleRad),
                centerA.Y + radiusPx * Math.Sin(_currentAngleRad)
            );

            // 1. Update guide circle
            if (_previewCircle != null)
            {
                _previewCircle.Width = radiusPx * 2;
                _previewCircle.Height = radiusPx * 2;
                Canvas.SetLeft(_previewCircle, centerA.X - radiusPx);
                Canvas.SetTop(_previewCircle, centerA.Y - radiusPx);
            }

            // 2. Update radius line (from Needle center A to Pencil tip B)
            if (_diameterLine != null)
            {
                _diameterLine.X1 = centerA.X;
                _diameterLine.Y1 = centerA.Y;
                _diameterLine.X2 = pencilTipB.X;
                _diameterLine.Y2 = pencilTipB.Y;
            }

            // 3. Update Center Needle Point (22x22px)
            if (_centerPoint != null)
            {
                Canvas.SetLeft(_centerPoint, centerA.X - 11);
                Canvas.SetTop(_centerPoint, centerA.Y - 11);
            }

            // 4. Update Realistic 2.5D Compa Geometry (In-place Point updates -> Zero GC allocations)
            UpdateCompassVisualGeometry(centerA, pencilTipB, radiusPx);

            // 5. Update Live Sweep Ink Path (when swinging the Compa pencil)
            UpdateLiveSweepPath(centerA, radiusPx);

            // 6. Update Handles on Guide Circle
            UpdateHandlePositions(radiusPx);

            // 7. Update Control Panel position (placed below circle so it never blocks the Compa)
            if (_controlPanel != null)
            {
                Rect visibleBounds = QASmartTouch.Helpers.BoundsHelper.GetVisibleCanvasBounds(_mainDashboard.MainInteractiveBoard);
                double minX = visibleBounds.Left + 16;
                double maxX = Math.Max(minX, visibleBounds.Right - 330);
                double panelX = Math.Clamp(centerA.X - 150, minX, maxX);
                double panelY = centerA.Y + radiusPx + 26;
                if (panelY + 95 > visibleBounds.Bottom)
                {
                    panelY = Math.Max(visibleBounds.Top + 16, centerA.Y + 28);
                }
                Canvas.SetLeft(_controlPanel, panelX);
                Canvas.SetTop(_controlPanel, panelY);
            }

            // 8. Update Radius TextBox
            if (_radiusInput != null && !_radiusInput.IsKeyboardFocused)
            {
                _radiusInput.Text = _currentRadiusCm.ToString("F1");
            }
        }

        private void UpdateCompassVisualGeometry(Point centerA, Point pencilTipB, double radiusPx)
        {
            if (_compassCanvas == null) return;

            // Midpoint between Needle tip A and Pencil tip B
            Point midM = new Point((centerA.X + pencilTipB.X) * 0.5, (centerA.Y + pencilTipB.Y) * 0.5);

            // 2.5D Upright Hinge position H:
            double legHeight = Math.Clamp(130.0 + radiusPx * 0.42, 150.0, 265.0);
            double sinT = Math.Sin(_currentAngleRad);
            double cosT = Math.Cos(_currentAngleRad);

            Point hingeH = new Point(
                midM.X - 34.0 * sinT,
                midM.Y - legHeight - 0.26 * radiusPx * sinT * sinT
            );

            // --- A. Left/Center Needle Leg (H -> A) ---
            Vector vHA = centerA - hingeH;
            double lenHA = Math.Max(1.0, vHA.Length);
            Vector uHA = vHA / lenHA;
            Vector nHA = new Vector(-uHA.Y, uHA.X);

            Point kneeA = hingeH + vHA * 0.80;
            Point collarA = hingeH + vHA * 0.90;

            SetQuadPoints(_needleLegBody,
                hingeH + nHA * 8.5,
                hingeH - nHA * 8.5,
                kneeA - nHA * 4.5,
                kneeA + nHA * 4.5);

            SetQuadPoints(_needleChuck,
                kneeA + nHA * 5.5,
                kneeA - nHA * 5.5,
                collarA - nHA * 3.0,
                collarA + nHA * 3.0);

            SetTrianglePoints(_needlePin,
                collarA + nHA * 2.2,
                collarA - nHA * 2.2,
                centerA);

            // --- B. Right/Outer Pencil & Pencil Leg (H -> Clamp -> B) ---
            Vector rawPencilUp = new Vector((hingeH.X - pencilTipB.X) + 22.0 * cosT, (hingeH.Y - pencilTipB.Y) - 28.0);
            double lenPencilUp = Math.Max(1.0, rawPencilUp.Length);
            Vector uPenUp = rawPencilUp / lenPencilUp;
            Vector nPen = new Vector(-uPenUp.Y, uPenUp.X);

            // Clamp position on the Pencil barrel (45px up from tip B)
            Point clampP = pencilTipB + uPenUp * 45.0;

            // Steel Pencil Leg from Hinge H to Clamp P
            Vector vHC = clampP - hingeH;
            double lenHC = Math.Max(1.0, vHC.Length);
            Vector uHC = vHC / lenHC;
            Vector nHC = new Vector(-uHC.Y, uHC.X);

            SetQuadPoints(_pencilLegBody,
                hingeH + nHC * 8.5,
                hingeH - nHC * 8.5,
                clampP - nHC * 5.0,
                clampP + nHC * 5.0);

            // --- C. Middle Threaded Bow Bar & Adjusting Wheel ---
            Point bowLeft = hingeH + (centerA - hingeH) * 0.45;
            Point bowRight = hingeH + (clampP - hingeH) * 0.52;
            Vector vBow = bowRight - bowLeft;
            double lenBow = Math.Max(1.0, vBow.Length);
            Vector uBow = vBow / lenBow;
            Vector nBow = new Vector(-uBow.Y, uBow.X);

            Point bowStart = bowLeft - uBow * 10.0;
            Point bowEnd = bowRight + uBow * 10.0;
            Point bowMid = new Point((bowLeft.X + bowRight.X) * 0.5, (bowLeft.Y + bowRight.Y) * 0.5);

            if (_bowBarLine != null)
            {
                _bowBarLine.X1 = bowStart.X;
                _bowBarLine.Y1 = bowStart.Y;
                _bowBarLine.X2 = bowEnd.X;
                _bowBarLine.Y2 = bowEnd.Y;
            }

            if (_bowThreadLine != null)
            {
                _bowThreadLine.X1 = bowStart.X;
                _bowThreadLine.Y1 = bowStart.Y;
                _bowThreadLine.X2 = bowEnd.X;
                _bowThreadLine.Y2 = bowEnd.Y;
            }

            SetQuadPoints(_bowWheel,
                bowMid - uBow * 5.0 + nBow * 11.0,
                bowMid + uBow * 5.0 + nBow * 11.0,
                bowMid + uBow * 5.0 - nBow * 11.0,
                bowMid - uBow * 5.0 - nBow * 11.0);

            if (_bowWheelHitArea != null)
            {
                Canvas.SetLeft(_bowWheelHitArea, bowMid.X - 20);
                Canvas.SetTop(_bowWheelHitArea, bowMid.Y - 20);
            }

            // --- D. Realistic Colored Pencil Segments ---
            Point pLeadEnd = pencilTipB + uPenUp * 11.0;
            Point pWoodEnd = pencilTipB + uPenUp * 26.0;
            Point pBarrelEnd = pencilTipB + uPenUp * 92.0;
            Point pFerruleEnd = pencilTipB + uPenUp * 102.0;
            Point pEraserEnd = pencilTipB + uPenUp * 114.0;

            if (_pencilLeadTip != null)
            {
                _pencilLeadTip.Fill = _activeStrokeBrush;
                SetTrianglePoints(_pencilLeadTip,
                    pencilTipB,
                    pLeadEnd + nPen * 2.6,
                    pLeadEnd - nPen * 2.6);
            }

            SetQuadPoints(_pencilWoodCone,
                pLeadEnd + nPen * 2.6,
                pLeadEnd - nPen * 2.6,
                pWoodEnd - nPen * 6.8,
                pWoodEnd + nPen * 6.8);

            SetQuadPoints(_pencilBarrel,
                pWoodEnd + nPen * 6.8,
                pWoodEnd - nPen * 6.8,
                pBarrelEnd - nPen * 6.8,
                pBarrelEnd + nPen * 6.8);

            if (_pencilColorStripe != null)
            {
                _pencilColorStripe.Fill = _activeStrokeBrush;
                SetQuadPoints(_pencilColorStripe,
                    pWoodEnd + nPen * 2.5,
                    pWoodEnd - nPen * 2.5,
                    pBarrelEnd - nPen * 2.5,
                    pBarrelEnd + nPen * 2.5);
            }

            SetQuadPoints(_pencilFerrule,
                pBarrelEnd + nPen * 6.8,
                pBarrelEnd - nPen * 6.8,
                pFerruleEnd - nPen * 6.8,
                pFerruleEnd + nPen * 6.8);

            SetQuadPoints(_pencilEraser,
                pFerruleEnd + nPen * 6.8,
                pFerruleEnd - nPen * 6.8,
                pEraserEnd - nPen * 6.8,
                pEraserEnd + nPen * 6.8);

            // Pencil Clamp Collar & Screw around clampP
            Point cBot = pencilTipB + uPenUp * 38.0;
            Point cTop = pencilTipB + uPenUp * 53.0;
            SetQuadPoints(_pencilClampCollar,
                cBot + nPen * 9.2,
                cBot - nPen * 9.2,
                cTop - nPen * 9.2,
                cTop + nPen * 9.2);

            if (_pencilClampScrew != null)
            {
                Canvas.SetLeft(_pencilClampScrew, clampP.X - 5);
                Canvas.SetTop(_pencilClampScrew, clampP.Y - 5);
            }

            // --- E. Top Grip Peg & Pivot Hinge Yoke at H ---
            SetQuadPoints(_topGripPeg,
                new Point(hingeH.X - 6.5, hingeH.Y - 8),
                new Point(hingeH.X + 6.5, hingeH.Y - 8),
                new Point(hingeH.X + 5.0, hingeH.Y - 38),
                new Point(hingeH.X - 5.0, hingeH.Y - 38));

            if (_topHingeOuter != null)
            {
                Canvas.SetLeft(_topHingeOuter, hingeH.X - 14);
                Canvas.SetTop(_topHingeOuter, hingeH.Y - 14);
            }

            if (_topHingeInner != null)
            {
                Canvas.SetLeft(_topHingeInner, hingeH.X - 6);
                Canvas.SetTop(_topHingeInner, hingeH.Y - 6);
            }

            // --- F. Swing Badge near Pencil Tip ---
            if (_pencilTipBadge != null)
            {
                Point badgePos = pencilTipB - uPenUp * 16.0;
                Canvas.SetLeft(_pencilTipBadge, badgePos.X - 13);
                Canvas.SetTop(_pencilTipBadge, badgePos.Y - 13);
            }

            // --- G. Update Interactive Hit Zones ---
            if (_hingeHitPolygon?.Points != null && _hingeHitPolygon.Points.Count >= 6)
            {
                _hingeHitPolygon.Points[0] = new Point(hingeH.X - 24, hingeH.Y - 44);
                _hingeHitPolygon.Points[1] = new Point(hingeH.X + 24, hingeH.Y - 44);
                _hingeHitPolygon.Points[2] = hingeH + nHA * 16;
                _hingeHitPolygon.Points[3] = centerA + nHA * 14;
                _hingeHitPolygon.Points[4] = centerA - nHA * 14;
                _hingeHitPolygon.Points[5] = hingeH - nHA * 16;
            }

            Point hitTip = pencilTipB - uPenUp * 28.0;
            Point hitTop = pEraserEnd + uPenUp * 12.0;
            SetQuadPoints(_pencilHitPolygon,
                hitTip + nPen * 24.0,
                hitTip - nPen * 24.0,
                hitTop - nPen * 22.0,
                hitTop + nPen * 22.0);

            // --- H. Update Size & Angle Label Badge ---
            if (_sizeLabel != null)
            {
                if (_isSwingingPencil)
                {
                    double sweptDeg = Math.Min(360.0, Math.Abs(_sweepAccumulatedRad) * 180.0 / Math.PI);
                    _sizeLabel.Text = $"R = {_currentRadiusCm:F1}cm • Đang quay: {sweptDeg:F0}°";
                }
                else
                {
                    _sizeLabel.Text = $"R = {_currentRadiusCm:F1}cm (⌀ {_currentRadiusCm * 2:F1}cm)";
                }
            }

            if (_sizeLabelBorder != null)
            {
                Canvas.SetLeft(_sizeLabelBorder, bowMid.X - 58);
                Canvas.SetTop(_sizeLabelBorder, bowMid.Y + 14);
            }
        }

        private void UpdateLiveSweepPath(Point centerA, double radiusPx)
        {
            if (_liveSweepPath == null) return;

            if (!_isSwingingPencil || Math.Abs(_sweepAccumulatedRad) < 0.02)
            {
                _liveSweepPath.Data = null;
                return;
            }

            _liveSweepPath.Stroke = _activeStrokeBrush;
            _liveSweepPath.StrokeThickness = _strokeThickness;
            _liveSweepPath.Data = BuildArcGeometry(centerA, radiusPx, _sweepStartAngleRad, _sweepAccumulatedRad);
        }

        private static Geometry BuildArcGeometry(Point center, double radius, double startAngleRad, double sweepAngleRad)
        {
            double clampedSweep = Math.Clamp(sweepAngleRad, -Math.PI * 2 + 0.001, Math.PI * 2 - 0.001);
            double midAngleRad = startAngleRad + clampedSweep * 0.5;
            double endAngleRad = startAngleRad + clampedSweep;

            Point startPt = new Point(center.X + radius * Math.Cos(startAngleRad), center.Y + radius * Math.Sin(startAngleRad));
            Point midPt = new Point(center.X + radius * Math.Cos(midAngleRad), center.Y + radius * Math.Sin(midAngleRad));
            Point endPt = new Point(center.X + radius * Math.Cos(endAngleRad), center.Y + radius * Math.Sin(endAngleRad));

            SweepDirection dir = clampedSweep >= 0 ? SweepDirection.Clockwise : SweepDirection.Counterclockwise;

            var figure = new PathFigure { StartPoint = startPt, IsClosed = false, IsFilled = false };
            figure.Segments.Add(new ArcSegment(midPt, new Size(radius, radius), 0, false, dir, true));
            figure.Segments.Add(new ArcSegment(endPt, new Size(radius, radius), 0, false, dir, true));

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            return geometry;
        }

        private void UpdateHandlePositions(double radiusPx)
        {
            if (_topHandle != null)
            {
                Canvas.SetLeft(_topHandle, _circleCenter.X - 7);
                Canvas.SetTop(_topHandle, _circleCenter.Y - radiusPx - 7);
            }

            if (_rightHandle != null)
            {
                Canvas.SetLeft(_rightHandle, _circleCenter.X + radiusPx - 7);
                Canvas.SetTop(_rightHandle, _circleCenter.Y - 7);
            }

            if (_bottomHandle != null)
            {
                Canvas.SetLeft(_bottomHandle, _circleCenter.X - 7);
                Canvas.SetTop(_bottomHandle, _circleCenter.Y + radiusPx - 7);
            }

            if (_leftHandle != null)
            {
                Canvas.SetLeft(_leftHandle, _circleCenter.X - radiusPx - 7);
                Canvas.SetTop(_leftHandle, _circleCenter.Y - 7);
            }
        }

        #endregion

        #region Move Compa Events (Top Hinge & Center Needle Point - Touch Isolated)

        private void CenterPoint_StartDrag(UIElement? element, Point startPos)
        {
            if (_mainDashboard == null) return;
            _isDraggingCircle = true;
            _activeMoveElement = element;
            _dragStartPoint = startPos;
        }

        private void CenterPoint_MoveDrag(Point currentPoint)
        {
            if (!_isDraggingCircle || _mainDashboard == null) return;

            double deltaX = currentPoint.X - _dragStartPoint.X;
            double deltaY = currentPoint.Y - _dragStartPoint.Y;
            if (Math.Abs(deltaX) < 0.25 && Math.Abs(deltaY) < 0.25) return;

            _circleCenter = new Point(_circleCenter.X + deltaX, _circleCenter.Y + deltaY);
            _dragStartPoint = currentPoint;

            UpdateCircle();
        }

        private void CenterPoint_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Ignore promoted mouse events when interacting via Touch screen
            if (_moveTouchId != null || e.StylusDevice != null)
            {
                e.Handled = true;
                return;
            }
            if (_mainDashboard == null || sender is not UIElement el) return;

            CenterPoint_StartDrag(el, e.GetPosition(_mainDashboard.MainInteractiveBoard));
            el.CaptureMouse();
            e.Handled = true;
        }

        private void CenterPoint_MouseMove(object sender, MouseEventArgs e)
        {
            if (_moveTouchId != null || e.StylusDevice != null) return;
            if (!_isDraggingCircle || _mainDashboard == null) return;

            CenterPoint_MoveDrag(e.GetPosition(_mainDashboard.MainInteractiveBoard));
            e.Handled = true;
        }

        private void CenterPoint_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_moveTouchId != null || e.StylusDevice != null)
            {
                e.Handled = true;
                return;
            }
            _isDraggingCircle = false;
            _activeMoveElement?.ReleaseMouseCapture();
            _activeMoveElement = null;
            e.Handled = true;
        }

        private void CenterPoint_TouchDown(object? sender, TouchEventArgs e)
        {
            if (_mainDashboard == null || sender is not UIElement el) return;
            if (_moveTouchId != null)
            {
                e.Handled = true;
                return;
            }
            _moveTouchId = e.TouchDevice.Id;
            CenterPoint_StartDrag(el, e.GetTouchPoint(_mainDashboard.MainInteractiveBoard).Position);
            el.CaptureTouch(e.TouchDevice);
            e.Handled = true;
        }

        private void CenterPoint_TouchMove(object? sender, TouchEventArgs e)
        {
            if (!_isDraggingCircle || _mainDashboard == null || _moveTouchId != e.TouchDevice.Id) return;
            CenterPoint_MoveDrag(e.GetTouchPoint(_mainDashboard.MainInteractiveBoard).Position);
            e.Handled = true;
        }

        private void CenterPoint_TouchUp(object? sender, TouchEventArgs e)
        {
            if (_moveTouchId != e.TouchDevice.Id) return;
            _isDraggingCircle = false;
            _moveTouchId = null;
            if (sender is UIElement el && e.TouchDevice.Captured == el)
            {
                el.ReleaseTouchCapture(e.TouchDevice);
            }
            _activeMoveElement = null;
            e.Handled = true;
        }

        private void CenterPoint_LostTouchCapture(object? sender, TouchEventArgs e)
        {
            if (_moveTouchId == e.TouchDevice.Id)
            {
                _isDraggingCircle = false;
                _moveTouchId = null;
                _activeMoveElement = null;
            }
        }

        #endregion

        #region Pencil Swing-to-Draw Events (Direct Compa Rotation - Touch Isolated)

        private static double NormalizeAngleDelta(double deltaRad)
        {
            while (deltaRad > Math.PI) deltaRad -= Math.PI * 2;
            while (deltaRad <= -Math.PI) deltaRad += Math.PI * 2;
            return deltaRad;
        }

        private void Pencil_StartDrag(Point startPos)
        {
            if (_mainDashboard == null) return;

            SyncPenSettingsFromDashboard();

            _isSwingingPencil = true;
            _currentAngleRad = Math.Atan2(startPos.Y - _circleCenter.Y, startPos.X - _circleCenter.X);
            _sweepStartAngleRad = _currentAngleRad;
            _lastSwingAngleRad = _currentAngleRad;
            _sweepAccumulatedRad = 0.0;

            UpdateCircle();
        }

        private void Pencil_MoveDrag(Point currentPoint)
        {
            if (!_isSwingingPencil || _mainDashboard == null) return;

            double dx = currentPoint.X - _circleCenter.X;
            double dy = currentPoint.Y - _circleCenter.Y;
            if (dx * dx + dy * dy < 16.0) return;

            double newAngle = Math.Atan2(dy, dx);
            double delta = NormalizeAngleDelta(newAngle - _lastSwingAngleRad);
            if (Math.Abs(delta) < 0.002) return;

            _sweepAccumulatedRad = Math.Clamp(_sweepAccumulatedRad + delta, -Math.PI * 2, Math.PI * 2);
            _lastSwingAngleRad = newAngle;
            _currentAngleRad = newAngle;

            UpdateCircle();
        }

        private void Pencil_EndDrag()
        {
            if (!_isSwingingPencil) return;
            _isSwingingPencil = false;
            _swingTouchId = null;

            if (_liveSweepPath != null)
            {
                _liveSweepPath.Data = null;
            }

            double sweptDeg = Math.Abs(_sweepAccumulatedRad) * 180.0 / Math.PI;
            double startAngle = _sweepStartAngleRad;
            double sweepAngle = _sweepAccumulatedRad;
            _sweepAccumulatedRad = 0.0;

            UpdateCircle();

            if (sweptDeg >= 345.0)
            {
                CommitFullCircleToBoard(closeToolAfterDraw: false);
            }
            else if (sweptDeg >= 8.0)
            {
                CommitArcToBoard(startAngle, sweepAngle);
            }
        }

        private void Pencil_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_swingTouchId != null || e.StylusDevice != null)
            {
                e.Handled = true;
                return;
            }
            if (_mainDashboard == null || sender is not UIElement el) return;
            Pencil_StartDrag(e.GetPosition(_mainDashboard.MainInteractiveBoard));
            el.CaptureMouse();
            e.Handled = true;
        }

        private void Pencil_MouseMove(object sender, MouseEventArgs e)
        {
            if (_swingTouchId != null || e.StylusDevice != null) return;
            if (!_isSwingingPencil || _mainDashboard == null) return;
            Pencil_MoveDrag(e.GetPosition(_mainDashboard.MainInteractiveBoard));
            e.Handled = true;
        }

        private void Pencil_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_swingTouchId != null || e.StylusDevice != null)
            {
                e.Handled = true;
                return;
            }
            (sender as UIElement)?.ReleaseMouseCapture();
            Pencil_EndDrag();
            e.Handled = true;
        }

        private void Pencil_TouchDown(object? sender, TouchEventArgs e)
        {
            if (_mainDashboard == null || sender is not UIElement el) return;
            if (_swingTouchId != null)
            {
                e.Handled = true;
                return;
            }
            _swingTouchId = e.TouchDevice.Id;
            Pencil_StartDrag(e.GetTouchPoint(_mainDashboard.MainInteractiveBoard).Position);
            el.CaptureTouch(e.TouchDevice);
            e.Handled = true;
        }

        private void Pencil_TouchMove(object? sender, TouchEventArgs e)
        {
            if (!_isSwingingPencil || _mainDashboard == null || _swingTouchId != e.TouchDevice.Id) return;
            Pencil_MoveDrag(e.GetTouchPoint(_mainDashboard.MainInteractiveBoard).Position);
            e.Handled = true;
        }

        private void Pencil_TouchUp(object? sender, TouchEventArgs e)
        {
            if (_swingTouchId != e.TouchDevice.Id) return;
            if (sender is UIElement el && e.TouchDevice.Captured == el)
            {
                el.ReleaseTouchCapture(e.TouchDevice);
            }
            Pencil_EndDrag();
            e.Handled = true;
        }

        private void Pencil_LostTouchCapture(object? sender, TouchEventArgs e)
        {
            if (_swingTouchId == e.TouchDevice.Id)
            {
                Pencil_EndDrag();
            }
        }

        #endregion

        #region Radius Handle Events (Delta-Based & Touch Isolated)

        private void Handle_StartDrag(UIElement handle, Point startPos)
        {
            _isDraggingHandle = true;
            _activeHandle = handle;
            _resizeStartRadiusCm = _currentRadiusCm;
            double dx = startPos.X - _circleCenter.X;
            double dy = startPos.Y - _circleCenter.Y;
            _resizeStartDistPx = Math.Sqrt(dx * dx + dy * dy);
        }

        private void Handle_MoveDrag(Point currentPoint)
        {
            if (!_isDraggingHandle || _activeHandle == null || _mainDashboard == null) return;

            double dx = currentPoint.X - _circleCenter.X;
            double dy = currentPoint.Y - _circleCenter.Y;
            double currentDistPx = Math.Sqrt(dx * dx + dy * dy);

            // Delta-based radius calculation: zero initial jump and smooth 1:1 tracking
            double deltaPx = currentDistPx - _resizeStartDistPx;
            if (_activeHandle == _bowWheelHitArea)
            {
                deltaPx *= 1.5;
            }

            double newRadiusCm = Math.Clamp(_resizeStartRadiusCm + (deltaPx / CM_TO_PX), 1.0, 20.0);
            if (Math.Abs(newRadiusCm - _currentRadiusCm) < 0.01) return;

            _currentRadiusCm = newRadiusCm;
            UpdateCircle();
        }

        private void Handle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Block promoted mouse events on interactive flat panels
            if (_resizeTouchId != null || e.StylusDevice != null)
            {
                e.Handled = true;
                return;
            }
            if (sender is UIElement handle && _mainDashboard != null)
            {
                Handle_StartDrag(handle, e.GetPosition(_mainDashboard.MainInteractiveBoard));
                handle.CaptureMouse();
                e.Handled = true;
            }
        }

        private void Handle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_resizeTouchId != null || e.StylusDevice != null) return;
            if (!_isDraggingHandle || _activeHandle == null || _mainDashboard == null) return;

            Handle_MoveDrag(e.GetPosition(_mainDashboard.MainInteractiveBoard));
            e.Handled = true;
        }

        private void Handle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_resizeTouchId != null || e.StylusDevice != null)
            {
                e.Handled = true;
                return;
            }
            _isDraggingHandle = false;
            _activeHandle?.ReleaseMouseCapture();
            _activeHandle = null;
            e.Handled = true;
        }

        private void Handle_TouchDown(object? sender, TouchEventArgs e)
        {
            if (sender is not UIElement handle || _mainDashboard == null) return;
            if (_resizeTouchId != null)
            {
                e.Handled = true;
                return;
            }
            _resizeTouchId = e.TouchDevice.Id;
            Handle_StartDrag(handle, e.GetTouchPoint(_mainDashboard.MainInteractiveBoard).Position);
            handle.CaptureTouch(e.TouchDevice);
            e.Handled = true;
        }

        private void Handle_TouchMove(object? sender, TouchEventArgs e)
        {
            if (!_isDraggingHandle || _mainDashboard == null || _resizeTouchId != e.TouchDevice.Id) return;
            Handle_MoveDrag(e.GetTouchPoint(_mainDashboard.MainInteractiveBoard).Position);
            e.Handled = true;
        }

        private void Handle_TouchUp(object? sender, TouchEventArgs e)
        {
            if (_resizeTouchId != e.TouchDevice.Id) return;
            _isDraggingHandle = false;
            _resizeTouchId = null;
            if (sender is UIElement handle && e.TouchDevice.Captured == handle)
            {
                handle.ReleaseTouchCapture(e.TouchDevice);
            }
            _activeHandle = null;
            e.Handled = true;
        }

        private void Handle_LostTouchCapture(object? sender, TouchEventArgs e)
        {
            if (_resizeTouchId == e.TouchDevice.Id)
            {
                _isDraggingHandle = false;
                _resizeTouchId = null;
                _activeHandle = null;
            }
        }

        #endregion

        #region Commit Circle / Arc to Board & Button Events

        private void btnDraw_Click(object sender, RoutedEventArgs e)
        {
            CommitFullCircleToBoard(closeToolAfterDraw: true);
        }

        private void CommitFullCircleToBoard(bool closeToolAfterDraw)
        {
            if (_mainDashboard == null) return;

            try
            {
                SyncPenSettingsFromDashboard();
                double radiusPx = _currentRadiusCm * CM_TO_PX;

                var container = new Canvas
                {
                    Width = radiusPx * 2,
                    Height = radiusPx * 2
                };

                var circle = new Ellipse
                {
                    Width = radiusPx * 2,
                    Height = radiusPx * 2,
                    Stroke = new SolidColorBrush(_strokeColor),
                    StrokeThickness = _strokeThickness,
                    Fill = null,
                    IsHitTestVisible = false,
                    SnapsToDevicePixels = true
                };
                Canvas.SetLeft(circle, 0);
                Canvas.SetTop(circle, 0);
                container.Children.Add(circle);

                var centerMark = CreateCenterMark(radiusPx);
                centerMark.IsHitTestVisible = false;
                container.Children.Add(centerMark);

                Canvas.SetLeft(container, _circleCenter.X - radiusPx);
                Canvas.SetTop(container, _circleCenter.Y - radiusPx);
                Panel.SetZIndex(container, QASmartTouch.Helpers.ZIndexConstants.UserContentBase);

                _mainDashboard.MainInteractiveBoard.Children.Add(container);

                try
                {
                    _mainDashboard.RecordToolDrawAction(container, $"Circle: radius={_currentRadiusCm:F1}cm");
                }
                catch { }

                ShowTemporaryMessage($"✓ Đã vẽ đường tròn bán kính R = {_currentRadiusCm:F1}cm");
                System.Media.SystemSounds.Asterisk.Play();

                if (closeToolAfterDraw)
                {
                    RemoveAllFromCanvas();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi vẽ hình tròn: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CommitArcToBoard(double startAngleRad, double sweepAngleRad)
        {
            if (_mainDashboard == null) return;

            try
            {
                SyncPenSettingsFromDashboard();
                double radiusPx = _currentRadiusCm * CM_TO_PX;

                var container = new Canvas
                {
                    Width = radiusPx * 2,
                    Height = radiusPx * 2
                };

                var localCenter = new Point(radiusPx, radiusPx);
                var arcPath = new Path
                {
                    Stroke = new SolidColorBrush(_strokeColor),
                    StrokeThickness = _strokeThickness,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    Fill = null,
                    Data = BuildArcGeometry(localCenter, radiusPx, startAngleRad, sweepAngleRad),
                    IsHitTestVisible = false,
                    SnapsToDevicePixels = true
                };
                container.Children.Add(arcPath);

                var centerMark = CreateCenterMark(radiusPx);
                centerMark.IsHitTestVisible = false;
                container.Children.Add(centerMark);

                Canvas.SetLeft(container, _circleCenter.X - radiusPx);
                Canvas.SetTop(container, _circleCenter.Y - radiusPx);
                Panel.SetZIndex(container, QASmartTouch.Helpers.ZIndexConstants.UserContentBase);

                _mainDashboard.MainInteractiveBoard.Children.Add(container);

                double deg = Math.Abs(sweepAngleRad) * 180.0 / Math.PI;
                try
                {
                    _mainDashboard.RecordToolDrawAction(container, $"Arc: radius={_currentRadiusCm:F1}cm, angle={deg:F0}deg");
                }
                catch { }

                ShowTemporaryMessage($"✓ Đã vẽ cung tròn {deg:F0}° (R = {_currentRadiusCm:F1}cm)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CommitArcToBoard error: {ex.Message}");
            }
        }

        private Ellipse CreateCenterMark(double radiusPx)
        {
            double centerX = radiusPx;
            double centerY = radiusPx;
            double dotRadius = Math.Clamp(_strokeThickness * 0.55, 1.8, 2.8);

            var centerDot = new Ellipse
            {
                Width = dotRadius * 2,
                Height = dotRadius * 2,
                Fill = new SolidColorBrush(_strokeColor),
                Tag = "CompassCenterDot",
                IsHitTestVisible = false,
                SnapsToDevicePixels = true
            };
            Canvas.SetLeft(centerDot, centerX - dotRadius);
            Canvas.SetTop(centerDot, centerY - dotRadius);

            return centerDot;
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
                if (double.TryParse(_radiusInput?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double radiusCm))
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

            UnwireMoveElementEvents(_centerPoint);
            UnwireMoveElementEvents(_hingeHitPolygon);
            UnwirePencilSwingEvents(_pencilHitPolygon);
            UnwireRadiusHandleEvents(_bowWheelHitArea);

            foreach (var handle in new[] { _topHandle, _rightHandle, _bottomHandle, _leftHandle })
            {
                UnwireRadiusHandleEvents(handle);
            }

            if (_previewCircle != null) _mainDashboard.MainInteractiveBoard.Children.Remove(_previewCircle);
            if (_liveSweepPath != null) _mainDashboard.MainInteractiveBoard.Children.Remove(_liveSweepPath);
            if (_compassCanvas != null) _mainDashboard.MainInteractiveBoard.Children.Remove(_compassCanvas);
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

            if (_mainDashboard.ActiveCircleDrawingTool == this)
            {
                _mainDashboard.ActiveCircleDrawingTool = null;
            }

            _previewCircle = null;
            _liveSweepPath = null;
            _compassCanvas = null;
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
            _activeHandle = null;
            _activeMoveElement = null;
            _mainDashboard = null;
        }

        private void ShowTemporaryMessage(string message)
        {
            if (_mainDashboard == null) return;

            var canvas = _mainDashboard.MainInteractiveBoard;

            Border messageBox = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(230, 16, 185, 129)),
                CornerRadius = new CornerRadius(6),
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

        private void btnMove_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_panelTouchId != null || e.StylusDevice != null)
            {
                e.Handled = true;
                return;
            }
            if (_mainDashboard == null || _controlPanel == null) return;

            _isDraggingPanel = true;
            _panelDragStart = e.GetPosition(_mainDashboard.MainInteractiveBoard);

            if (sender is UIElement el)
                el.CaptureMouse();

            e.Handled = true;
        }

        private void btnMove_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_panelTouchId != null || e.StylusDevice != null) return;
            if (!_isDraggingPanel || _mainDashboard == null || _controlPanel == null) return;

            var currentPoint = e.GetPosition(_mainDashboard.MainInteractiveBoard);

            double deltaX = currentPoint.X - _panelDragStart.X;
            double deltaY = currentPoint.Y - _panelDragStart.Y;

            _circleCenter = new Point(_circleCenter.X + deltaX, _circleCenter.Y + deltaY);
            _panelDragStart = currentPoint;

            UpdateCircle();
            e.Handled = true;
        }

        private void btnMove_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_panelTouchId != null || e.StylusDevice != null)
            {
                e.Handled = true;
                return;
            }
            if (_isDraggingPanel)
            {
                _isDraggingPanel = false;

                if (sender is UIElement el)
                    el.ReleaseMouseCapture();

                e.Handled = true;
            }
        }

        private int? _panelTouchId = null;

        private void btnMove_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            if (_mainDashboard == null || _controlPanel == null) return;

            if (sender is UIElement el)
            {
                _panelTouchId = e.TouchDevice.Id;
                _isDraggingPanel = true;
                _panelDragStart = e.GetTouchPoint(_mainDashboard.MainInteractiveBoard).Position;
                el.CaptureTouch(e.TouchDevice);
                e.Handled = true;
            }
        }

        private void btnMove_TouchMove(object sender, TouchEventArgs e)
        {
            if (!_isDraggingPanel || _mainDashboard == null || _controlPanel == null) return;
            if (_panelTouchId != e.TouchDevice.Id) return;

            var currentPoint = e.GetTouchPoint(_mainDashboard.MainInteractiveBoard).Position;

            double deltaX = currentPoint.X - _panelDragStart.X;
            double deltaY = currentPoint.Y - _panelDragStart.Y;

            _circleCenter = new Point(_circleCenter.X + deltaX, _circleCenter.Y + deltaY);
            _panelDragStart = currentPoint;

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
            SyncPenSettingsFromDashboard();
        }

        public void UpdatePenSettings(Color color, double thickness)
        {
            _strokeColor = color;
            _strokeThickness = Math.Max(2.0, thickness);
            _activeStrokeBrush = new SolidColorBrush(_strokeColor);
            UpdateCircle();
        }

        #endregion
    }
}
