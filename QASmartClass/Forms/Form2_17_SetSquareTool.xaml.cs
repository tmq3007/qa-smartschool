using System;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;
using System.Windows.Threading;
using Serilog;
using QASmartTouch.Interfaces;

namespace QASmartTouch.Forms
{
    public partial class Form2_17_SetSquareTool : Window, IStemTool
    {
        #region Fields

        private Form2_MainDashboard _mainDashboard;

        // Set Square State
        private SetSquareState _state;

        // Drag State for Window
        private bool _isDragging = false;
        private Point _dragStartPoint;
        private double _originalLeft = 0;
        private double _originalTop = 0;

        // Flip State
        private bool _isFlipped = false;

        // Drawing states
        private Point? _drawStartPoint = null;
        private Line? _previewLine = null;
        private bool _isDrawing = false;

        // ✨ PHASE 3: Counter for drawn lines (auto-move after 3 lines)
        private int _drawnLinesCount = 0;


        // Brush Settings
        private Color _strokeColor = Color.FromRgb(34, 34, 34);
        private double _strokeThickness = 2.0;
        private PenLineCap _strokeCap = PenLineCap.Round;

        #endregion

        #region Constructor

        public Form2_17_SetSquareTool(Form2_MainDashboard mainDashboard)
        {
            InitializeComponent();
            // QC_4.2_TOUCH_PIPELINE: STEM Window — WPF tự cô lập, KHÔNG cần ApplyTouchIsolation

            _mainDashboard = mainDashboard;

            // Load brush settings
            LoadBrushSettings();

            // Initialize state with default mode (30-60-90)
            _state = new SetSquareState
            {
                Mode = SetSquareMode.Triangle_30_60_90,
                RotationAngle = 0,
                IsFlipped = false
            };
        }

        private void LoadBrushSettings()
        {
            // Load brush configuration from settings/preferences
            // Using default values for now
            _strokeColor = Color.FromRgb(34, 34, 34); // #222222
            _strokeThickness = 2.0;
            _strokeCap = PenLineCap.Round;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Center window on screen
            var workArea = SystemParameters.WorkArea;
            this.Left = (workArea.Width - this.Width) / 2;
            this.Top = (workArea.Height - this.Height) / 2;

            // Draw set square
            DrawSetSquare();
        }

        #endregion

        #region Quick Draw Triangle Methods

        // 1. Vẽ tam giác 30-60-90° chuẩn (theo 3 cạnh eke)
        private void BtnDrawStandard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Triangle vertices in eke coordinate system (relative to SetSquareVisual)
                Point p1 = new Point(0, 0);      // Top-left corner (90° angle)
                Point p2 = new Point(660, 0);    // Top-right corner (30° angle)
                Point p3 = new Point(660, 240);  // Bottom-right corner (60° angle)

                // Transform to screen coordinates
                Point p1Screen = SetSquareVisual.PointToScreen(p1);
                Point p2Screen = SetSquareVisual.PointToScreen(p2);
                Point p3Screen = SetSquareVisual.PointToScreen(p3);

                // Transform from screen to MainBoard coordinates
                Point tp1 = _mainDashboard.MainInteractiveBoard.PointFromScreen(p1Screen);
                Point tp2 = _mainDashboard.MainInteractiveBoard.PointFromScreen(p2Screen);
                Point tp3 = _mainDashboard.MainInteractiveBoard.PointFromScreen(p3Screen);

                // Draw 3 lines forming triangle
                DrawLineOnMainCanvas(tp1, tp2); // Cạnh huyền (20cm)
                DrawLineOnMainCanvas(tp2, tp3); // Cạnh góc vuông lớn (16cm)
                DrawLineOnMainCanvas(tp3, tp1); // Cạnh góc vuông nhỏ (5cm)

                AutoMoveSetSquareAway();
                ShowTemporaryMessage("✓ Đã vẽ tam giác 30-60-90°");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi vẽ tam giác: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 2. Vẽ tam giác đều 60-60-60° (dùng cạnh huyền làm cạnh đều)
        private void BtnDrawEquilateral_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Use hypotenuse length (660px = 20cm) for equilateral triangle
                Point p1 = new Point(0, 0);
                Point p2 = new Point(660, 0);

                // Calculate third vertex for equilateral triangle
                // Height = side * sqrt(3)/2
                double height = 660 * Math.Sqrt(3) / 2; // ~571.87px
                Point p3 = new Point(330, height); // Midpoint horizontally, height above

                // Transform via screen coordinates
                Point p1Screen = SetSquareVisual.PointToScreen(p1);
                Point p2Screen = SetSquareVisual.PointToScreen(p2);
                Point p3Screen = SetSquareVisual.PointToScreen(p3);

                Point tp1 = _mainDashboard.MainInteractiveBoard.PointFromScreen(p1Screen);
                Point tp2 = _mainDashboard.MainInteractiveBoard.PointFromScreen(p2Screen);
                Point tp3 = _mainDashboard.MainInteractiveBoard.PointFromScreen(p3Screen);

                // Draw equilateral triangle
                DrawLineOnMainCanvas(tp1, tp2);
                DrawLineOnMainCanvas(tp2, tp3);
                DrawLineOnMainCanvas(tp3, tp1);

                AutoMoveSetSquareAway();
                ShowTemporaryMessage("✓ Đã vẽ tam giác đều 60°");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi vẽ tam giác đều: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 3. Vẽ tam giác vuông cân 45-45-90°
        private void BtnDrawIsosceles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Use 480px (16cm) as equal side length
                double side = 480;
                Point p1 = new Point(0, 0);      // Right angle vertex
                Point p2 = new Point(side, 0);   // First leg
                Point p3 = new Point(0, side);   // Second leg (equal length)

                // Transform via screen coordinates
                Point p1Screen = SetSquareVisual.PointToScreen(p1);
                Point p2Screen = SetSquareVisual.PointToScreen(p2);
                Point p3Screen = SetSquareVisual.PointToScreen(p3);

                Point tp1 = _mainDashboard.MainInteractiveBoard.PointFromScreen(p1Screen);
                Point tp2 = _mainDashboard.MainInteractiveBoard.PointFromScreen(p2Screen);
                Point tp3 = _mainDashboard.MainInteractiveBoard.PointFromScreen(p3Screen);

                DrawLineOnMainCanvas(tp1, tp2);
                DrawLineOnMainCanvas(tp2, tp3);
                DrawLineOnMainCanvas(tp3, tp1);

                AutoMoveSetSquareAway();
                ShowTemporaryMessage("✓ Đã vẽ tam giác vuông cân 45°");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi vẽ tam giác vuông cân: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 4. Vẽ tam giác góc vuông ở đỉnh (lật ngược)
        private void BtnDrawInverted_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Inverted triangle: right angle at top
                Point p1 = new Point(330, 0);     // Top vertex (90° angle)
                Point p2 = new Point(0, 480);     // Bottom-left
                Point p3 = new Point(660, 480);   // Bottom-right

                // Transform via screen coordinates
                Point p1Screen = SetSquareVisual.PointToScreen(p1);
                Point p2Screen = SetSquareVisual.PointToScreen(p2);
                Point p3Screen = SetSquareVisual.PointToScreen(p3);

                Point tp1 = _mainDashboard.MainInteractiveBoard.PointFromScreen(p1Screen);
                Point tp2 = _mainDashboard.MainInteractiveBoard.PointFromScreen(p2Screen);
                Point tp3 = _mainDashboard.MainInteractiveBoard.PointFromScreen(p3Screen);

                DrawLineOnMainCanvas(tp1, tp2);
                DrawLineOnMainCanvas(tp2, tp3);
                DrawLineOnMainCanvas(tp3, tp1);

                AutoMoveSetSquareAway();
                ShowTemporaryMessage("✓ Đã vẽ tam giác ngược");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi vẽ tam giác ngược: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 5. Vẽ lưới tam giác (3 cột x 2 hàng = 6 tam giác)
        private void BtnDrawPattern_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double triWidth = 220;  // Mỗi tam giác rộng 220px (~7.3cm)
                double triHeight = 190; // Chiều cao 190px (~6.3cm)

                // Draw 3x2 grid of triangles
                for (int row = 0; row < 2; row++)
                {
                    for (int col = 0; col < 3; col++)
                    {
                        double baseX = col * triWidth;
                        double baseY = row * triHeight;

                        // Alternating upward/downward triangles
                        if ((row + col) % 2 == 0)
                        {
                            // Upward triangle ▲
                            Point p1 = new Point(baseX, baseY + triHeight);
                            Point p2 = new Point(baseX + triWidth, baseY + triHeight);
                            Point p3 = new Point(baseX + triWidth / 2, baseY);

                            Point p1Screen = SetSquareVisual.PointToScreen(p1);
                            Point p2Screen = SetSquareVisual.PointToScreen(p2);
                            Point p3Screen = SetSquareVisual.PointToScreen(p3);

                            Point tp1 = _mainDashboard.MainInteractiveBoard.PointFromScreen(p1Screen);
                            Point tp2 = _mainDashboard.MainInteractiveBoard.PointFromScreen(p2Screen);
                            Point tp3 = _mainDashboard.MainInteractiveBoard.PointFromScreen(p3Screen);

                            DrawLineOnMainCanvas(tp1, tp2);
                            DrawLineOnMainCanvas(tp2, tp3);
                            DrawLineOnMainCanvas(tp3, tp1);
                        }
                        else
                        {
                            // Downward triangle ▼
                            Point p1 = new Point(baseX, baseY);
                            Point p2 = new Point(baseX + triWidth, baseY);
                            Point p3 = new Point(baseX + triWidth / 2, baseY + triHeight);

                            Point p1Screen = SetSquareVisual.PointToScreen(p1);
                            Point p2Screen = SetSquareVisual.PointToScreen(p2);
                            Point p3Screen = SetSquareVisual.PointToScreen(p3);

                            Point tp1 = _mainDashboard.MainInteractiveBoard.PointFromScreen(p1Screen);
                            Point tp2 = _mainDashboard.MainInteractiveBoard.PointFromScreen(p2Screen);
                            Point tp3 = _mainDashboard.MainInteractiveBoard.PointFromScreen(p3Screen);

                            DrawLineOnMainCanvas(tp1, tp2);
                            DrawLineOnMainCanvas(tp2, tp3);
                            DrawLineOnMainCanvas(tp3, tp1);
                        }
                    }
                }

                AutoMoveSetSquareAway();
                ShowTemporaryMessage("✓ Đã vẽ lưới 6 tam giác");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi vẽ lưới tam giác: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Helper method: Draw a single line on MainBoard
        private void DrawLineOnMainCanvas(Point start, Point end)
        {
            Line line = new Line
            {
                X1 = start.X,
                Y1 = start.Y,
                X2 = end.X,
                Y2 = end.Y,
                Stroke = new SolidColorBrush(_strokeColor),
                StrokeThickness = _strokeThickness,
                StrokeStartLineCap = _strokeCap,
                StrokeEndLineCap = _strokeCap
            };

            _mainDashboard.MainInteractiveBoard.Children.Add(line);
            _mainDashboard.RecordToolDrawAction(line, "SetSquare: draw line");
        }

        #endregion

        #region Drawing Methods

        private void DrawSetSquare()
        {
            SetSquareCanvas.Children.Clear();

            // Update content size for current mode
            UpdateContentSizeForMode();

            // Update triangle shape
            UpdateTriangleShape();

            // Draw ruler markings on all edges
            DrawRulerMarkings();

            // Draw angle markers
            DrawAngleMarkers();

            // Restore UI elements
            RestoreUIElements();

            // Update mode display
            UpdateModeDisplay();
        }

        private void UpdateTriangleShape()
        {
            PointCollection points = new PointCollection();

            switch (_state.Mode)
            {
                case SetSquareMode.Triangle_30_60_90:
                    // Hypotenuse = 22cm = 660px, Long leg = 18cm = 540px, Short leg = 8cm = 240px
                    // Tam giác bắt đầu từ (0,0) - Top-left
                    points.Add(new Point(0, 0));       // Top-left (30°)
                    points.Add(new Point(660, 0));     // Top-right (90°)
                    points.Add(new Point(660, 240));   // Bottom-right (60°)

                    // Keep window size from XAML (900×600)
                    // No need to change window size

                    // Update handle position for large triangle (trong Canvas coordinate)
                    Canvas.SetLeft(HandleHole, 615);
                    Canvas.SetTop(HandleHole, 24);
                    break;

                case SetSquareMode.Triangle_45_45_90:
                    // Hypotenuse = 720px, equal sides = 720/√2 ≈ 509px
                    // Right angle at top-left, 45° at bottom-right and top-right
                    points.Add(new Point(0, 0));       // Top-left (90°)
                    points.Add(new Point(509, 0));     // Top-right (45°)
                    points.Add(new Point(0, 509));     // Bottom-left (45°)

                    // Keep window size from XAML (900×600)
                    // No need to change window size

                    // Update handle position for large triangle
                    Canvas.SetLeft(HandleHole, 230);
                    Canvas.SetTop(HandleHole, 230);
                    break;
            }

            TriangleBody.Points = points;
        }

        private void DrawRulerMarkings()
        {
            Point[] vertices = GetTriangleVertices();

            // Draw markings on all 3 edges - tự động hướng vào trong
            DrawRulerOnEdge(vertices[0], vertices[1]);  // Edge 1 (bottom)
            DrawRulerOnEdge(vertices[1], vertices[2]);  // Edge 2 (hypotenuse)
            DrawRulerOnEdge(vertices[2], vertices[0]);  // Edge 3 (right)
        }

        private void DrawRulerOnEdge(Point start, Point end)
        {
            Vector edge = end - start;
            double edgeLength = edge.Length;
            edge.Normalize();

            // Tính perpendicular ban đầu (vuông góc sang trái)
            Vector perpendicular = new Vector(-edge.Y, edge.X);

            // Tính center của tam giác
            Point[] vertices = GetTriangleVertices();
            Point center = new Point(
                (vertices[0].X + vertices[1].X + vertices[2].X) / 3.0,
                (vertices[0].Y + vertices[1].Y + vertices[2].Y) / 3.0
            );

            // Điểm giữa cạnh
            Point edgeMid = new Point(
                (start.X + end.X) / 2.0,
                (start.Y + end.Y) / 2.0
            );

            // Vector từ giữa cạnh đến center
            Vector toCenter = new Vector(
                center.X - edgeMid.X,
                center.Y - edgeMid.Y
            );

            // Kiểm tra perpendicular có hướng vào center không (dot product)
            double dotProduct = perpendicular.X * toCenter.X + perpendicular.Y * toCenter.Y;
            if (dotProduct < 0)
            {
                // Nếu hướng ra ngoài, đảo ngược lại
                perpendicular = -perpendicular;
            }

            // Tính margin cho từng cạnh để hiển thị đúng độ dài:
            // Cạnh dài (660px): 16cm = 480px → startMargin = 120px, endMargin = 90px
            // Cạnh huyền (~706px): 20cm = 600px → startMargin = 26px, endMargin = 76px (tổng 102px → hiển thị 0-20)
            // Cạnh ngắn (240px): 5cm = 150px → margin = 45px (không thay đổi)
            double startMarginPx, endMarginPx;
            if (edgeLength > 650) // Cạnh dài hoặc cạnh huyền
            {
                if (edgeLength > 700) // Cạnh huyền
                {
                    startMarginPx = 26;  // Gần điểm A (giao với cạnh dài)
                    endMarginPx = 76;    // Gần điểm B → tổng 102px, khoảng 604px cho phép vẽ đến 20cm
                }
                else // Cạnh dài (đáy)
                {
                    startMarginPx = 130; // 90px + 30px = 120px (thêm 1cm từ giao điểm)
                    endMarginPx = 80;    // Giữ nguyên để đủ 16cm
                }
            }
            else // Cạnh ngắn
            {
                startMarginPx = 45;
                endMarginPx = 45;
            }
            
            double startOffset = startMarginPx;
            double endOffset = edgeLength - endMarginPx;

            // Tỷ lệ chuẩn: 1mm = 3px (giống thước kẻ)
            double pxPerMm = 3.0;
            int totalMM = (int)((endOffset - startOffset) / pxPerMm);

            // Vẽ vạch mỗi 1mm
            for (int i = 0; i <= totalMM; i++)
            {
                double positionOnEdge = startOffset + (i * pxPerMm);
                if (positionOnEdge > endOffset) break;

                Point tickBase = start + edge * positionOnEdge;

                double tickLength;
                double thickness;
                bool showNumber = false;

                // Vạch cm (mỗi 10mm) - giống thước kẻ
                if (i % 10 == 0)
                {
                    tickLength = 8;      // Dài nhất (thước kẻ: 20px, eke: 8px vì nhỏ hơn)
                    thickness = 1.5;
                    showNumber = true;
                }
                // Vạch 5mm - giống thước kẻ
                else if (i % 5 == 0)
                {
                    tickLength = 5;      // Trung bình (thước kẻ: 12px, eke: 5px)
                    thickness = 1.0;
                }
                // Vạch 1mm - giống thước kẻ
                else
                {
                    tickLength = 3;      // Ngắn nhất (thước kẻ: 7px, eke: 3px)
                    thickness = 0.6;
                }

                Point tickEnd = tickBase + perpendicular * tickLength;

                Line tick = new Line
                {
                    X1 = tickBase.X,
                    Y1 = tickBase.Y,
                    X2 = tickEnd.X,
                    Y2 = tickEnd.Y,
                    Stroke = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                    StrokeThickness = thickness
                };
                SetSquareCanvas.Children.Add(tick);

                // Số cm - giống thước kẻ
                if (showNumber)
                {
                    TextBlock number = new TextBlock
                    {
                        Text = (i / 10).ToString(),
                        FontSize = 11,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
                    };

                    // Tăng khoảng cách từ vạch để không bị đè lên
                    Point numberPos = tickBase + perpendicular * (tickLength + 8);
                    Canvas.SetLeft(number, numberPos.X - 4);
                    Canvas.SetTop(number, numberPos.Y - 5);
                    SetSquareCanvas.Children.Add(number);
                }
            }
        }

        private void DrawAngleMarkers()
        {
            Point[] vertices = GetTriangleVertices();

            switch (_state.Mode)
            {
                case SetSquareMode.Triangle_30_60_90:
                    // 30° angle at top-left
                    DrawAngleArc(vertices[0], 30, 10, "30°");

                    // 90° angle at top-right
                    DrawRightAngleSquare(vertices[1], 6);

                    // 60° angle at bottom-right
                    DrawAngleArc(vertices[2], 60, 10, "60°");
                    break;

                case SetSquareMode.Triangle_45_45_90:
                    // 90° angle at top
                    DrawRightAngleSquare(vertices[0], 6);

                    // 45° angle at bottom-right
                    DrawAngleArc(vertices[1], 45, 10, "45°");

                    // 45° angle at bottom-left
                    DrawAngleArc(vertices[2], 45, 10, "45°");
                    break;
            }
        }

        private void DrawAngleArc(Point vertex, double angleDegrees, double radius, string label)
        {
            // Draw small arc
            double angleRad = angleDegrees * Math.PI / 180;
            Path arc = new Path
            {
                Stroke = new SolidColorBrush(Color.FromRgb(30, 64, 175)),
                StrokeThickness = 1
            };

            PathGeometry pathGeometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure
            {
                StartPoint = new Point(vertex.X + radius, vertex.Y)
            };

            ArcSegment arcSegment = new ArcSegment
            {
                Point = new Point(vertex.X + radius * Math.Cos(angleRad), vertex.Y - radius * Math.Sin(angleRad)),
                Size = new Size(radius, radius),
                SweepDirection = SweepDirection.Counterclockwise
            };

            pathFigure.Segments.Add(arcSegment);
            pathGeometry.Figures.Add(pathFigure);
            arc.Data = pathGeometry;

            SetSquareCanvas.Children.Add(arc);

            // Draw label
            TextBlock angleLabel = new TextBlock
            {
                Text = label,
                FontSize = 8,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 64, 175))
            };
            Canvas.SetLeft(angleLabel, vertex.X + radius + 2);
            Canvas.SetTop(angleLabel, vertex.Y - 6);
            SetSquareCanvas.Children.Add(angleLabel);
        }

        private void DrawRightAngleSquare(Point vertex, double size)
        {
            Rectangle square = new Rectangle
            {
                Width = size,
                Height = size,
                Stroke = new SolidColorBrush(Color.FromRgb(30, 64, 175)),
                StrokeThickness = 1.2,
                Fill = Brushes.Transparent
            };
            Canvas.SetLeft(square, vertex.X - size);
            Canvas.SetTop(square, vertex.Y - size);
            SetSquareCanvas.Children.Add(square);
        }

        private Point[] GetTriangleVertices()
        {
            Point[] vertices = new Point[3];

            for (int i = 0; i < 3; i++)
            {
                vertices[i] = TriangleBody.Points[i];
            }

            return vertices;
        }

        private void RestoreUIElements()
        {
            // Re-add handle hole
            if (SetSquareCanvas.Children.Contains(HandleHole))
            {
                SetSquareCanvas.Children.Remove(HandleHole);
            }
            SetSquareCanvas.Children.Add(HandleHole);

            // Re-add mode display
            if (SetSquareCanvas.Children.Contains(ModeDisplay))
            {
                SetSquareCanvas.Children.Remove(ModeDisplay);
            }
            SetSquareCanvas.Children.Add(ModeDisplay);
        }

        private void UpdateModeDisplay()
        {
            ModeText.Text = _state.Mode == SetSquareMode.Triangle_30_60_90 
                ? "30-60-90°" 
                : "45-45-90°";
        }

        #endregion

        #region Button Events - Move

        private void btnMove_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isDragging = true;
                _dragStartPoint = PointToScreen(e.GetPosition(this));

                // CaptureMouse: nhận mọi events dù di nhanh
                if (sender is UIElement el)
                    el.CaptureMouse();

                SetSquareLayerRoot.CacheMode = new BitmapCache
                {
                    RenderAtScale = 2.0,
                    EnableClearType = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetEdgeMode(SetSquareLayerRoot, EdgeMode.Aliased);

                e.Handled = true;
            }
        }

        private void btnMove_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                // Lấy vị trí chuột hiện tại trên màn hình
                var currentScreenPoint = PointToScreen(e.GetPosition(this));
                
                // Tính khoảng cách di chuyển so với lần cập nhật trước
                double offsetX = currentScreenPoint.X - _dragStartPoint.X;
                double offsetY = currentScreenPoint.Y - _dragStartPoint.Y;
                
                // Cập nhật vị trí window
                this.Left += offsetX;
                this.Top += offsetY;
                
                // Cập nhật điểm tham chiếu cho lần di chuyển tiếp theo
                _dragStartPoint = currentScreenPoint;
                
                e.Handled = true;
            }
        }

        private void btnMove_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;

                // Nhả capture
                if (sender is UIElement el)
                    el.ReleaseMouseCapture();

                SetSquareLayerRoot.CacheMode = null;
                RenderOptions.SetEdgeMode(SetSquareLayerRoot, EdgeMode.Unspecified);
                SetSquareLayerRoot.InvalidateVisual();

                _drawnLinesCount = 0;

                e.Handled = true;
            }
        }

        #endregion

        #region Button Events - Other

        private void btnSwitch_Click(object sender, RoutedEventArgs e)
        {
            // Toggle between modes
            if (_state.Mode == SetSquareMode.Triangle_30_60_90)
            {
                _state.Mode = SetSquareMode.Triangle_45_45_90;
                ShowTemporaryMessage("Đã chuyển sang Eke 45-45-90°");
            }
            else
            {
                _state.Mode = SetSquareMode.Triangle_30_60_90;
                ShowTemporaryMessage("Đã chuyển sang Eke 30-60-90°");
            }

            DrawSetSquare();
        }

        // ═══════════════════════════════════════════════════════
        //  [LOI_VID_54] ROTATION VỚI ANIMATION + SNAP + BITMAP CACHE
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// [LOI_VID_54] Danh sách góc snap chuẩn.
        /// Bao gồm 15°, 75°, 105°, 165° cho Hình học lớp 7 (phản biện TS. Đặng Thị Lan).
        /// </summary>
        private static readonly double[] SNAP_ANGLES = {
            0, 15, 30, 45, 60, 75, 90, 105, 120, 135, 150, 165,
            180, 195, 210, 225, 240, 255, 270, 285, 300, 315, 330, 345
        };
        private const double SNAP_THRESHOLD = 2.0; // ±2°

        /// <summary>
        /// [LOI_VID_54] Bắt góc thông minh — tự động snap vào góc chuẩn gần nhất nếu trong ngưỡng ±2°.
        /// </summary>
        private static double SnapAngle(double angle)
        {
            // Normalize to 0-360
            angle = ((angle % 360) + 360) % 360;

            foreach (var snap in SNAP_ANGLES)
            {
                if (Math.Abs(angle - snap) <= SNAP_THRESHOLD)
                    return snap;
            }
            // Check wrap-around 360 → 0
            if (Math.Abs(angle - 360) <= SNAP_THRESHOLD)
                return 0;

            return angle;
        }

        /// <summary>
        /// [LOI_VID_54] Xoay với animation mượt + BitmapCache + SnapAngle.
        /// Bật BitmapCache RenderAtScale=2.0 khi animation, tắt khi xong.
        /// </summary>
        private void AnimateRotation(double targetAngle)
        {
            // [LOI_VID_54] Bật BitmapCache để GPU render khi xoay
            SetSquareLayerRoot.CacheMode = new BitmapCache
            {
                RenderAtScale = 2.0,
                EnableClearType = true,
                SnapsToDevicePixels = true
            };

            var animation = new DoubleAnimation(
                SetSquareRotate.Angle, targetAngle,
                TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            animation.Completed += (s, e) =>
            {
                // [LOI_VID_54] Tắt BitmapCache khi xoay xong → giữ vạch chia sắc nét
                SetSquareLayerRoot.CacheMode = null;
                RenderOptions.SetEdgeMode(SetSquareLayerRoot, EdgeMode.Unspecified);

                // Reset animation để cho phép set Angle trực tiếp sau
                SetSquareRotate.BeginAnimation(RotateTransform.AngleProperty, null);
                SetSquareRotate.Angle = targetAngle;
            };

            SetSquareRotate.BeginAnimation(RotateTransform.AngleProperty, animation);

            Log.Debug("[LOI_VID_54] Rotation animated to {Angle}° (snapped)", targetAngle);
        }

        private void btnRotate_Click(object sender, RoutedEventArgs e)
        {
            _state.RotationAngle += 15;
            if (_state.RotationAngle >= 360)
                _state.RotationAngle -= 360;

            // [LOI_VID_54] Snap + animation
            _state.RotationAngle = SnapAngle(_state.RotationAngle);
            AnimateRotation(_state.RotationAngle);
            ShowTemporaryMessage($"Xoay thuận: {_state.RotationAngle}°");
        }

        private void btnRotateBack_Click(object sender, RoutedEventArgs e)
        {
            _state.RotationAngle -= 15;
            if (_state.RotationAngle < 0)
                _state.RotationAngle += 360;

            // [LOI_VID_54] Snap + animation
            _state.RotationAngle = SnapAngle(_state.RotationAngle);
            AnimateRotation(_state.RotationAngle);
            ShowTemporaryMessage($"Xoay ngược: {_state.RotationAngle}°");
        }

        /// <summary>
        /// Cập nhật kích thước SetSquareLayerRoot theo mode (eke centered trong Window lớn)
        /// </summary>
        private void UpdateContentSizeForMode()
        {
            if (_state.Mode == SetSquareMode.Triangle_30_60_90)
            {
                SetSquareLayerRoot.Width = 860;
                SetSquareLayerRoot.Height = 340;
            }
            else // 45-45-90
            {
                SetSquareLayerRoot.Width = 700;
                SetSquareLayerRoot.Height = 600;
            }
        }

        private void btnFlip_Click(object sender, RoutedEventArgs e)
        {
            _state.IsFlipped = !_state.IsFlipped;
            
            if (_state.IsFlipped)
            {
                SetSquareScale.ScaleY = -1;
                FlipNumberLabels(-1); // Lật ngược các số để dễ đọc
                ShowTemporaryMessage("Đã đảo vạch chia");
            }
            else
            {
                SetSquareScale.ScaleY = 1;
                FlipNumberLabels(1); // Khôi phục các số về bình thường
                ShowTemporaryMessage("Đã khôi phục vạch chia");
            }
        }

        private void FlipNumberLabels(double scaleY)
        {
            // Tìm và lật tất cả các TextBlock số cm trên Canvas
            foreach (UIElement child in SetSquareCanvas.Children)
            {
                if (child is TextBlock textBlock)
                {
                    // Tạo transform để lật ngược text
                    var transform = new ScaleTransform
                    {
                        ScaleY = scaleY,
                        CenterX = textBlock.ActualWidth / 2,
                        CenterY = textBlock.ActualHeight / 2
                    };
                    textBlock.RenderTransform = transform;
                }
                // Lật cả ModeDisplay (Border chứa "30-60-90°")
                else if (child is Border border && border.Name == "ModeDisplay")
                {
                    var transform = new ScaleTransform
                    {
                        ScaleY = scaleY,
                        CenterX = border.ActualWidth / 2,
                        CenterY = border.ActualHeight / 2
                    };
                    border.RenderTransform = transform;
                }
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Drawing Logic

        private void SetSquareCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Nếu đang drag eke, không vẽ
            if (_isDragging)
            {
                e.Handled = true;
                return;
            }

            // Nếu đang drag window (chuột trái giữ), không vẽ
            if (e.LeftButton == MouseButtonState.Pressed && this.IsMouseCaptured)
            {
                return;
            }

            // BƯỚC 1: Bắt đầu vẽ với Mouse Capture
            // Lấy tọa độ từ sender (có thể là Polygon hoặc Canvas)
            UIElement element = sender as UIElement;
            if (element == null) return;

            _drawStartPoint = e.GetPosition(element);
            _isDrawing = true;

            // Capture mouse để luôn nhận MouseUp kể cả khi kéo ra ngoài
            Mouse.Capture(element);

            // Create preview line (nét đứt xanh mờ) trên Canvas
            _previewLine = new Line
            {
                X1 = _drawStartPoint.Value.X,
                Y1 = _drawStartPoint.Value.Y,
                X2 = _drawStartPoint.Value.X,
                Y2 = _drawStartPoint.Value.Y,
                Stroke = new SolidColorBrush(Color.FromArgb(128, 33, 150, 243)),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            SetSquareCanvas.Children.Add(_previewLine);
        }

        private void SetSquareCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            // Nếu đang drag eke, không vẽ
            if (_isDragging)
            {
                return;
            }

            // BƯỚC 2: Kéo vẽ realtime
            if (_isDrawing && _previewLine != null && _drawStartPoint.HasValue)
            {
                UIElement element = sender as UIElement;
                if (element == null) return;

                Point currentPoint = e.GetPosition(element);

                // Chỉ vẽ nếu di chuyển > 3px (tránh nhầm click)
                double distance = Math.Sqrt(
                    Math.Pow(currentPoint.X - _drawStartPoint.Value.X, 2) +
                    Math.Pow(currentPoint.Y - _drawStartPoint.Value.Y, 2)
                );

                if (distance > 3)
                {
                    // Update preview line
                    _previewLine.X2 = currentPoint.X;
                    _previewLine.Y2 = currentPoint.Y;
                }
            }
        }

        private void SetSquareCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // BƯỚC 3: Hoàn tất vẽ
            if (_isDrawing && _drawStartPoint.HasValue)
            {
                // Release mouse capture
                Mouse.Capture(null);

                UIElement element = sender as UIElement;
                if (element == null) return;

                // Lấy điểm cuối chính xác
                Point endPoint = e.GetPosition(element);

                // Remove preview line - ĐẢM BẢO xóa trước
                CleanupPreviewLine();

                // Check if it's not just a click (minimum distance)
                double distance = Math.Sqrt(
                    Math.Pow(endPoint.X - _drawStartPoint.Value.X, 2) +
                    Math.Pow(endPoint.Y - _drawStartPoint.Value.Y, 2)
                );

                if (distance > 5) // Minimum 5px to be considered a line
                {
                    // Vẽ đường thẳng từ điểm đầu đến điểm cuối
                    // Truyền element để biết tọa độ đang dựa trên element nào
                    DrawStraightLineOnMainCanvas(_drawStartPoint.Value, endPoint, element);
                }

                // Reset state
                _drawStartPoint = null;
                _isDrawing = false;
            }
        }

        private void SetSquareCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            // An toàn trạng thái - khi chuột rời canvas
            // Không hủy nếu đang vẽ và đã capture mouse (vẫn nhận MouseUp)
        }

        private void CleanupPreviewLine()
        {
            // Method riêng để đảm bảo preview line luôn bị xóa
            if (_previewLine != null)
            {
                SetSquareCanvas.Children.Remove(_previewLine);
                _previewLine = null;
            }
        }

        private void DrawStraightLineOnMainCanvas(Point start, Point end, UIElement sourceElement)
        {
            try
            {
                // LUÔN VẼ ĐƯỜNG THẲNG TỪ ĐIỂM ĐẦU ĐẾN ĐIỂM CUỐI
                // Không quan tâm đến quỹ đạo chuột di chuyển

                // Chuyển đổi tọa độ - Ưu tiên TransformToVisual
                Point startOnMain, endOnMain;

                try
                {
                    // Phương pháp 1: TransformToVisual (khi cùng visual tree)
                    GeneralTransform transform = sourceElement.TransformToVisual(_mainDashboard.MainInteractiveBoard);
                    startOnMain = transform.Transform(start);
                    endOnMain = transform.Transform(end);
                }
                catch
                {
                    // Phương pháp 2: Qua Screen coordinates (khác Window/overlay)
                    Point startOnScreen = sourceElement.PointToScreen(start);
                    Point endOnScreen = sourceElement.PointToScreen(end);
                    startOnMain = _mainDashboard.MainInteractiveBoard.PointFromScreen(startOnScreen);
                    endOnMain = _mainDashboard.MainInteractiveBoard.PointFromScreen(endOnScreen);
                }

                // Tạo Line với THÔNG SỐ ĐÃ LƯU
                Line line = new Line
                {
                    X1 = startOnMain.X,
                    Y1 = startOnMain.Y,
                    X2 = endOnMain.X,
                    Y2 = endOnMain.Y,
                    Stroke = new SolidColorBrush(_strokeColor),
                    StrokeThickness = _strokeThickness,
                    StrokeStartLineCap = _strokeCap,
                    StrokeEndLineCap = _strokeCap,
                    SnapsToDevicePixels = true
                };

                // Add to main canvas
                _mainDashboard.MainInteractiveBoard.Children.Add(line);
                _mainDashboard.RecordToolDrawAction(line, "SetSquare: draw edge line");

                // ✨ PHASE 4: Auto-move only after 3 lines
                _drawnLinesCount++;

                if (_drawnLinesCount >= 3)
                {
                    // Move after 3 lines
                    AutoMoveSetSquareAway();
                    ShowTemporaryMessage($"✓ Đã vẽ xong 3 đường");
                    _drawnLinesCount = 0; // Reset counter
                }
                else
                {
                    // Show progress
                    ShowTemporaryMessage($"✓ Đã vẽ ({_drawnLinesCount}/3)");
                }

                // Play sound feedback
                SystemSounds.Asterisk.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi vẽ đường: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AutoMoveSetSquareAway()
        {
            // Dịch eke theo chiều dọc dựa trên hướng vạch chia
            double moveDistance = 150;
            double newTop;

            if (_state.IsFlipped)
            {
                // Vạch chia hướng xuống dưới → Dịch LÊN TRÊN
                newTop = this.Top - moveDistance;

                // Đảm bảo không vượt ra ngoài mép trên màn hình
                if (newTop < 0)
                {
                    newTop = 50;
                }
            }
            else
            {
                // Vạch chia hướng lên trên → Dịch XUỐNG DƯỚI
                newTop = this.Top + moveDistance;

                // Đảm bảo không vượt ra ngoài mép dưới màn hình
                double screenHeight = SystemParameters.PrimaryScreenHeight;
                if (newTop + this.Height > screenHeight)
                {
                    newTop = screenHeight - this.Height - 50;
                }
            }

            this.Top = newTop;
        }

        #endregion

        #region Helper Methods

        private void ShowTemporaryMessage(string message)
        {
            Border messageBox = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(230, 30, 64, 175)),
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
                FontSize = 13,
                FontWeight = FontWeights.SemiBold
            };

            messageBox.Child = text;
            MainGrid.Children.Add(messageBox);

            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            timer.Tick += (s, e) =>
            {
                MainGrid.Children.Remove(messageBox);
                timer.Stop();
            };
            timer.Start();
        }

        /// <summary>
        /// ✨ PHASE 6: Teleport Eke to screen position with smooth animation
        /// </summary>
        public void TeleportToPosition(Point screenPosition)
        {
            try
            {
                // Get work area
                var workArea = SystemParameters.WorkArea;
                
                // Calculate target position (center Eke on click point)
                double targetLeft = screenPosition.X - (this.Width / 2);
                double targetTop = screenPosition.Y - (this.Height / 2);
                
                // Bounds checking (keep Eke on screen)
                targetLeft = Math.Max(0, Math.Min(targetLeft, workArea.Width - this.Width));
                targetTop = Math.Max(0, Math.Min(targetTop, workArea.Height - this.Height));
                
                // Create smooth animations
                var duration = TimeSpan.FromMilliseconds(300);
                var easing = new QuadraticEase { EasingMode = EasingMode.EaseOut };
                
                // Position animations
                var leftAnim = new DoubleAnimation
                {
                    To = targetLeft,
                    Duration = duration,
                    EasingFunction = easing
                };
                
                var topAnim = new DoubleAnimation
                {
                    To = targetTop,
                    Duration = duration,
                    EasingFunction = easing
                };
                
                // Flash effect (opacity)
                var opacityAnim = new DoubleAnimation
                {
                    From = 0.3,
                    To = 1.0,
                    Duration = duration,
                    EasingFunction = easing
                };
                
                // ✨ CRITICAL: Clear animations on complete to enable drag after teleport
                leftAnim.Completed += (s, e) =>
                {
                    this.BeginAnimation(Window.LeftProperty, null);
                    this.Left = targetLeft;
                };
                
                topAnim.Completed += (s, e) =>
                {
                    this.BeginAnimation(Window.TopProperty, null);
                    this.Top = targetTop;
                };
                
                opacityAnim.Completed += (s, e) =>
                {
                    this.BeginAnimation(Window.OpacityProperty, null);
                    this.Opacity = 1.0;
                };
                
                // Apply animations
                this.BeginAnimation(Window.LeftProperty, leftAnim);
                this.BeginAnimation(Window.TopProperty, topAnim);
                this.BeginAnimation(Window.OpacityProperty, opacityAnim);
                
                // Sound feedback
                SystemSounds.Asterisk.Play();
                
                // Visual feedback
                ShowTemporaryMessage("✓ Đã di chuyển");
                
                // ✨ Reset counter when teleporting
                _drawnLinesCount = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi teleport: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        // ═══════════════════════════════════════════════════════
        //  [CAI_TIEN_VID_10] IStemTool IMPLEMENTATION
        // ═══════════════════════════════════════════════════════

        #region IStemTool

        /// <inheritdoc />
        public string ToolDisplayName => "Ê-ke STEM";

        /// <inheritdoc />
        public DateTime LastActivatedTime { get; private set; } = DateTime.UtcNow;

        /// <inheritdoc />
        public void OnActivated()
        {
            LastActivatedTime = DateTime.UtcNow;
            Log.Debug("[CAI_TIEN_VID_10] {Tool} activated", ToolDisplayName);
        }

        /// <inheritdoc />
        public void OnDeactivated()
        {
            // Giải phóng BitmapCache
            SetSquareLayerRoot.CacheMode = null;

            // Reset drawing state
            _isDrawing = false;
            _drawStartPoint = null;
            if (_previewLine != null)
            {
                SetSquareCanvas.Children.Remove(_previewLine);
                _previewLine = null;
            }
            _drawnLinesCount = 0;

            Log.Debug("[CAI_TIEN_VID_10] {Tool} deactivated, resources released", ToolDisplayName);
        }

        /// <inheritdoc />
        public double GetMemoryUsageMB()
        {
            // Ước tính: base Window ~2MB + Canvas children ~0.5KB each
            double baseMB = 2.0;
            double childrenMB = SetSquareCanvas.Children.Count * 0.0005;
            double cacheMB = SetSquareLayerRoot.CacheMode != null ? 5.0 : 0;
            return baseMB + childrenMB + cacheMB;
        }

        #endregion
    }

    #region Supporting Classes

    public class SetSquareState
    {
        public SetSquareMode Mode { get; set; }
        public double RotationAngle { get; set; }
        public bool IsFlipped { get; set; }
    }

    public enum SetSquareMode
    {
        Triangle_30_60_90,
        Triangle_45_45_90
    }

    #endregion
}

