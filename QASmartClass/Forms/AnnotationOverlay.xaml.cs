using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace QASmartTouch.Forms
{
    public partial class AnnotationOverlay : Window
    {
        #region Win32 API for Click-Through & Custom Cursor

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [DllImport("user32.dll", EntryPoint = "CreateIconIndirect", SetLastError = true)]
        private static extern IntPtr CreateIconIndirect(ref ICONINFO icon);

        [DllImport("user32.dll", EntryPoint = "GetIconInfo", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);

        [DllImport("user32.dll", EntryPoint = "DestroyIcon", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        #endregion

        private bool _isPenMode = false;
        private bool _isDrawing = false;
        private bool _isEraseByClickMode = false;  // Chế độ xóa nét
        private bool _isErasing = false;           // Đang nhấn giữ để xóa liên tục
        private Point _lastErasePoint;
        private bool _hasLastErasePoint = false;
        private readonly List<UIElement> _currentEraseSessionElements = new();
        private Ellipse? _eraserPreview;
        private const double EraserRadius = 22.0;
        private static Cursor? _customEraserCursor;
        private Polyline? _currentStroke;
        private List<Polyline> _strokes = new List<Polyline>();
        private Managers.UndoRedoManager? _undoRedoManager;
        
        // Drawing properties
        private Color _currentColor = Colors.Red;
        private double _currentThickness = 3;
        private string _brushType = "Normal";
        private AnnotationTool _currentTool = AnnotationTool.Pen;
        
        // Event for toolbar repositioning
        public event EventHandler<ToolbarSideEventArgs>? ToolbarRepositionRequested;

        public AnnotationOverlay()
        {
            InitializeComponent();
            
            // Initialize Undo/Redo Manager
            _undoRedoManager = new Managers.UndoRedoManager();
            
            // Start in mouse mode (click-through)
            this.Loaded += AnnotationOverlay_Loaded;
            
            // Add keyboard shortcuts
            this.PreviewKeyDown += AnnotationOverlay_PreviewKeyDown;
            
            // Add double-click handler for toolbar repositioning
            // Use PreviewMouseDoubleClick to capture event even in click-through mode
            this.PreviewMouseDoubleClick += AnnotationOverlay_PreviewMouseDoubleClick;
            
            System.Diagnostics.Debug.WriteLine("✅ AnnotationOverlay initialized with Undo/Redo");
        }

        private void AnnotationOverlay_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+Z for Undo
            if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Undo();
                e.Handled = true;
            }
            // Ctrl+Y for Redo
            else if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Redo();
                e.Handled = true;
            }
        }
        
        private void AnnotationOverlay_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Get click position
            var clickPosition = e.GetPosition(this);
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            
            // Determine which side was clicked (left or right half)
            bool isLeftSide = clickPosition.X < (screenWidth / 2);
            
            System.Diagnostics.Debug.WriteLine($"🖱️ Double-click detected at X={clickPosition.X:F0}, Screen width={screenWidth:F0}, Side={( isLeftSide ? "LEFT" : "RIGHT")}");
            
            // Raise event to reposition toolbar
            ToolbarRepositionRequested?.Invoke(this, new ToolbarSideEventArgs(isLeftSide));
        }

        private void AnnotationOverlay_Loaded(object sender, RoutedEventArgs e)
        {
            // Set click-through initially
            SetClickThrough(true);
        }

        #region Click-Through Control

        private void SetClickThrough(bool enabled)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            
            if (enabled)
            {
                // Enable click-through
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
                System.Diagnostics.Debug.WriteLine("🖱️ Click-through ENABLED (Mouse Mode)");
            }
            else
            {
                // Disable click-through
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
                System.Diagnostics.Debug.WriteLine("✏️ Click-through DISABLED (Pen Mode)");
            }
        }

        #endregion

        #region Mode Control

        /// <summary>
        /// Chế độ Bút: overlay nhận click để vẽ (tắt click-through)
        /// </summary>
        public void SetPenMode(bool enabled)
        {
            _isPenMode = enabled;

            if (enabled)
            {
                // Bật pen: remove WS_EX_TRANSPARENT → overlay nhận mọi sự kiện chuột
                SetClickThrough(false);
                this.Visibility = Visibility.Visible;
                this.Cursor = Cursors.Pen;
                this.ForceCursor = true;
                DrawingCanvas.Cursor = Cursors.Pen;
                DrawingCanvas.ForceCursor = true;
                System.Diagnostics.Debug.WriteLine("✏️ Pen mode ON");
            }
            else
            {
                // Tắt pen: quay về click-through (trừ khi đang ở erase mode)
                if (!_isEraseByClickMode)
                    SetClickThrough(true);
                this.Cursor = Cursors.Arrow;
                this.ForceCursor = false;
                DrawingCanvas.Cursor = Cursors.Arrow;
                DrawingCanvas.ForceCursor = false;
                System.Diagnostics.Debug.WriteLine("✏️ Pen mode OFF");
            }
        }

        /// <summary>
        /// Chế độ Chuột: overlay trong suốt với click (WS_EX_TRANSPARENT),
        /// annotation vẫn hiển thị, nhưng click xưỹn qua xuống cửa sổ bên dưới.
        /// </summary>
        public void SetMouseMode(bool enabled)
        {
            if (enabled)
            {
                // Bật click-through: overlay hiện nhưng không chặn chuột
                _isPenMode = false;
                SetClickThrough(true);
                this.Visibility = Visibility.Visible; // vẫn hiện annotation
                this.Cursor = Cursors.Arrow;
                this.ForceCursor = false;
                System.Diagnostics.Debug.WriteLine("🖥️ Mouse mode ON – click-through enabled");
            }
            else
            {
                // Tắt mouse mode: giữ nguyên click-through (Pen/Erase sẽ tự xử lý)
                this.Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine("🖥️ Mouse mode OFF");
            }
        }

        /// <summary>
        /// Bật/tắt chế độ capture click mà không vào pen mode
        /// (Dùng cho chế độ Xóa Nét khi chưa chọn công cụ bút)
        /// </summary>
        public void SetClickCaptureOnly(bool enabled)
        {
            if (enabled)
            {
                // Cho phép nhận click (bỏ click-through)
                SetClickThrough(false);
                this.Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine("✅ Click capture enabled (erase mode)");
            }
            else
            {
                // Khôi phục click-through nếu không ở pen mode
                if (!_isPenMode)
                {
                    SetClickThrough(true);
                }
                System.Diagnostics.Debug.WriteLine("✅ Click capture disabled");
            }
        }

        public void SetTool(AnnotationTool tool)
        {
            _currentTool = tool;
            
            // Set cursor based on tool
            Cursor newCursor;
            switch (tool)
            {
                case AnnotationTool.Pen:
                case AnnotationTool.Highlighter:
                    newCursor = Cursors.Pen;
                    if (_eraserPreview != null) _eraserPreview.Visibility = Visibility.Collapsed;
                    break;
                case AnnotationTool.Eraser:
                    newCursor = GetOrCreateEraserCursor();
                    break;
                default:
                    newCursor = Cursors.Arrow;
                    if (_eraserPreview != null) _eraserPreview.Visibility = Visibility.Collapsed;
                    break;
            }
            
            // Force cursor for both Window and Canvas
            this.Cursor = newCursor;
            this.ForceCursor = true;
            DrawingCanvas.Cursor = newCursor;
            DrawingCanvas.ForceCursor = true;
            
            System.Diagnostics.Debug.WriteLine($"🔧 Tool changed to: {tool}, Cursor: {newCursor}");
        }

        public void SetColor(Color color)
        {
            _currentColor = color;
            System.Diagnostics.Debug.WriteLine($"🎨 Color changed to: {color}");
        }

        public void SetThickness(double thickness)
        {
            _currentThickness = thickness;
            System.Diagnostics.Debug.WriteLine($"📏 Thickness changed to: {thickness}");
        }

        public void SetBrushType(string brushType)
        {
            _brushType = string.IsNullOrEmpty(brushType) ? "Normal" : brushType;
            System.Diagnostics.Debug.WriteLine($"🖌️ AnnotationOverlay brush type set to: {_brushType}");
        }

        public string BrushType => _brushType;

        public bool HasBackground
        {
            get
            {
                return DrawingCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .Any(img => img.Tag?.ToString() == "BackgroundImage");
            }
        }

        public void SetBackground(System.Windows.Media.Imaging.BitmapSource screenshot)
        {
            try
            {
                // Remove old background image if exists
                var oldBackground = DrawingCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Tag?.ToString() == "BackgroundImage");
                if (oldBackground != null)
                {
                    DrawingCanvas.Children.Remove(oldBackground);
                }
                
                // Create background image
                var backgroundImage = new System.Windows.Controls.Image
                {
                    Source = screenshot,
                    Stretch = Stretch.Fill,
                    Width = SystemParameters.PrimaryScreenWidth,
                    Height = SystemParameters.PrimaryScreenHeight,
                    Tag = "BackgroundImage" // Mark as background
                };
                
                // Add to canvas at position 0,0 with lowest Z-index
                DrawingCanvas.Children.Insert(0, backgroundImage); // Insert at beginning
                System.Windows.Controls.Canvas.SetLeft(backgroundImage, 0);
                System.Windows.Controls.Canvas.SetTop(backgroundImage, 0);
                System.Windows.Controls.Canvas.SetZIndex(backgroundImage, -1000); // Behind everything
                
                System.Diagnostics.Debug.WriteLine("✅ Background image added to canvas");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error setting background: {ex.Message}");
            }
        }

        #endregion

        #region Mouse Events

        private void DrawingCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.StylusDevice != null) return; // Prevent duplicate handling from touch/stylus

            // Chế độ xóa nét: nhấn chuột để bắt đầu phiên xóa
            if (_isEraseByClickMode)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    _isErasing = true;
                    _hasLastErasePoint = false;
                    _currentEraseSessionElements.Clear();
                    DrawingCanvas.CaptureMouse();
                    var point = e.GetPosition(DrawingCanvas);
                    UpdateEraserPreview(point);
                    PerformErase(point);
                }
                return;
            }

            if (!_isPenMode || _currentTool == AnnotationTool.Eraser) return;

            _isDrawing = true;
            var pt = e.GetPosition(DrawingCanvas);
            StartDrawing(pt);
        }

        private void DrawingCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.StylusDevice != null) return; // Prevent duplicate handling from touch/stylus

            if (_isEraseByClickMode)
            {
                var point = e.GetPosition(DrawingCanvas);
                UpdateEraserPreview(point);
                if (_isErasing && e.LeftButton == MouseButtonState.Pressed)
                {
                    PerformErase(point);
                }
                return;
            }

            if (!_isDrawing) return;

            var pt = e.GetPosition(DrawingCanvas);
            ContinueDrawing(pt);
        }

        private void DrawingCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.StylusDevice != null) return; // Prevent duplicate handling from touch/stylus

            if (_isEraseByClickMode)
            {
                if (_isErasing)
                {
                    DrawingCanvas.ReleaseMouseCapture();
                    FinishEraseSession();
                }
                return;
            }

            if (!_isDrawing) return;

            EndDrawing();
        }

        private void DrawingCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_eraserPreview != null)
            {
                _eraserPreview.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region Stylus Events

        private void DrawingCanvas_StylusDown(object sender, StylusDownEventArgs e)
        {
            // Chế độ xóa nét trên màn hình cảm ứng / bút stylus
            if (_isEraseByClickMode)
            {
                _isErasing = true;
                _hasLastErasePoint = false;
                _currentEraseSessionElements.Clear();
                DrawingCanvas.CaptureStylus();
                var point = e.GetPosition(DrawingCanvas);
                UpdateEraserPreview(point);
                PerformErase(point);
                return;
            }

            if (!_isPenMode || _currentTool == AnnotationTool.Eraser) return;

            _isDrawing = true;
            var pt = e.GetPosition(DrawingCanvas);
            StartDrawing(pt);
        }

        private void DrawingCanvas_StylusMove(object sender, StylusEventArgs e)
        {
            if (_isEraseByClickMode)
            {
                var point = e.GetPosition(DrawingCanvas);
                UpdateEraserPreview(point);
                if (_isErasing)
                {
                    PerformErase(point);
                }
                return;
            }

            if (!_isDrawing) return;

            var pt = e.GetPosition(DrawingCanvas);
            ContinueDrawing(pt);
        }

        private void DrawingCanvas_StylusUp(object sender, StylusEventArgs e)
        {
            if (_isEraseByClickMode)
            {
                if (_isErasing)
                {
                    DrawingCanvas.ReleaseStylusCapture();
                    FinishEraseSession();
                }
                return;
            }

            if (!_isDrawing) return;

            EndDrawing();
        }

        #endregion

        #region Drawing Logic

        private void StartDrawing(Point point)
        {
            _currentStroke = new Polyline
            {
                Stroke = new SolidColorBrush(_currentColor),
                StrokeThickness = _currentThickness,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };

            _currentStroke.Points.Add(point);
            // ✨ DOT SUPPORT: Thêm điểm vi mô (+0.01px) để WPF render chấm tròn tức thì khi click chuột
            _currentStroke.Points.Add(new Point(point.X + 0.01, point.Y));
            DrawingCanvas.Children.Add(_currentStroke);
            _strokes.Add(_currentStroke);

            System.Diagnostics.Debug.WriteLine($"Started drawing at ({point.X:F0}, {point.Y:F0})");
        }

        private void ContinueDrawing(Point point)
        {
            if (_currentStroke != null)
            {
                // Nếu mới chỉ có 1 điểm gốc và 1 điểm vi mô preview, thay thế điểm vi mô bằng điểm thực tế đầu tiên
                if (_currentStroke.Points.Count == 2 && Math.Abs(_currentStroke.Points[1].X - _currentStroke.Points[0].X - 0.01) < 0.001 && _currentStroke.Points[1].Y == _currentStroke.Points[0].Y)
                {
                    _currentStroke.Points[1] = point;
                }
                else
                {
                    _currentStroke.Points.Add(point);
                }
            }
        }

        private void EndDrawing()
        {
            if (_currentStroke != null)
            {
                // ✨ DOT FALLBACK: Đảm bảo nếu chỉ có 1 điểm thì thêm điểm vi mô thứ hai
                if (_currentStroke.Points.Count == 1)
                {
                    Point pt = _currentStroke.Points[0];
                    _currentStroke.Points.Add(new Point(pt.X + 0.01, pt.Y));
                }

                // QC_4.2_SHAPE_RECOGNITION: Nhận diện nét vẽ hình học khi bật Bút nhận diện hình
                if (_brushType == "Shape" && _currentStroke.Points.Count >= 5)
                {
                    var recognizedShape = QASmartTouch.Helpers.ShapeRecognizer.TryRecognizeShape(_currentStroke, _currentColor, _currentThickness);
                    if (recognizedShape != null)
                    {
                        DrawingCanvas.Children.Remove(_currentStroke);
                        _strokes.Remove(_currentStroke);

                        DrawingCanvas.Children.Add(recognizedShape);
                        _undoRedoManager?.RecordAddAction(recognizedShape, "Nhận diện hình học");

                        System.Diagnostics.Debug.WriteLine($"📐 Shape recognized and converted successfully on DESKTOP");
                        _currentStroke = null;
                        _isDrawing = false;
                        return;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"✅ Completed stroke with {_currentStroke.Points.Count} points");
                
                // Record action for undo/redo
                _undoRedoManager?.RecordAddAction(_currentStroke, "Draw stroke");
                
                _currentStroke = null;
            }
            
            _isDrawing = false;
        }

        #endregion

        #region Eraser

        public void EraseAtPoint(Point point)
        {
            var elementsToRemove = new List<UIElement>();

            foreach (UIElement element in DrawingCanvas.Children)
            {
                if (element is Polyline stroke)
                {
                    // Check if point is near any point in the stroke
                    foreach (Point strokePoint in stroke.Points)
                    {
                        var distance = Math.Sqrt(
                            Math.Pow(point.X - strokePoint.X, 2) +
                            Math.Pow(point.Y - strokePoint.Y, 2)
                        );

                        if (distance < 20) // Eraser radius
                        {
                            elementsToRemove.Add(element);
                            break;
                        }
                    }
                }
            }

            foreach (var element in elementsToRemove)
            {
                DrawingCanvas.Children.Remove(element);
                if (element is Polyline stroke)
                {
                    _strokes.Remove(stroke);
                }
            }

            if (elementsToRemove.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"🧹 Erased {elementsToRemove.Count} strokes");
            }
        }

        #endregion

        #region Clear All

        public void ClearAll()
        {
            var count = DrawingCanvas.Children.Count;
            if (count > 0)
            {
                var elements = new List<UIElement>();
                foreach (UIElement child in DrawingCanvas.Children)
                {
                    elements.Add(child);
                }
                
                var batchActions = new List<Managers.UndoRedoAction>();
                foreach (var el in elements)
                {
                    batchActions.Add(new Managers.UndoRedoAction
                    {
                        Type = Managers.ActionType.Remove,
                        Element = el,
                        Parent = DrawingCanvas,
                        Description = "Clear All Element"
                    });
                }
                
                _undoRedoManager?.RecordBatchAction(batchActions, "Xóa sạch bảng");
                
                DrawingCanvas.Children.Clear();
                _strokes.Clear();
            }
            
            System.Diagnostics.Debug.WriteLine($"🧹 Cleared all ({count} elements)");
        }

        /// <summary>
        /// Bật/tắt chế độ xóa nét (hỗ trợ cả click từng nét và nhấn kéo giữ xóa liên tục)
        /// </summary>
        public void ToggleEraseByClickMode()
        {
            _isEraseByClickMode = !_isEraseByClickMode;

            if (_isEraseByClickMode)
            {
                // Vào chế độ xóa: chỉnh con trỏ sang hình cục tẩy và bật click-capture
                SetClickThrough(false);  // Phải nhận click
                Cursor eraserCursor = GetOrCreateEraserCursor();
                this.Cursor = eraserCursor;
                this.ForceCursor = true;
                DrawingCanvas.Cursor = eraserCursor;
                DrawingCanvas.ForceCursor = true;
                EnsureEraserPreview();
                ShowToast("🧹 Chế độ Xóa Nét – Nhấn và kéo qua nét để xóa | Nhấn lại để thoát", "#5C6BC0");
            }
            else
            {
                // Thoát chế độ xóa: khôi phục trạng thái cũ
                _isErasing = false;
                _hasLastErasePoint = false;
                if (_eraserPreview != null)
                {
                    _eraserPreview.Visibility = Visibility.Collapsed;
                }

                if (_isPenMode)
                {
                    this.Cursor = Cursors.Pen;
                    DrawingCanvas.Cursor = Cursors.Pen;
                }
                else
                {
                    SetClickThrough(true);
                    this.Cursor = Cursors.Arrow;
                    DrawingCanvas.Cursor = Cursors.Arrow;
                }
                ForceCursor = false;
                ShowToast("✅ Đã thoát chế độ Xóa Nét", "#388E3C");
            }
        }

        /// <summary>
        /// Thực hiện xóa liên tục khi di chuyển con trỏ (có nội suy điểm trung gian để không sót nét khi kéo nhanh)
        /// </summary>
        private void PerformErase(Point currentPoint)
        {
            if (!_isErasing) return;

            if (_hasLastErasePoint)
            {
                double dx = currentPoint.X - _lastErasePoint.X;
                double dy = currentPoint.Y - _lastErasePoint.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist > 10.0)
                {
                    int steps = (int)Math.Ceiling(dist / 10.0);
                    for (int i = 1; i <= steps; i++)
                    {
                        double t = (double)i / steps;
                        Point pt = new Point(_lastErasePoint.X + t * dx, _lastErasePoint.Y + t * dy);
                        EraseStrokesAtPoint(pt, EraserRadius);
                    }
                }
                else
                {
                    EraseStrokesAtPoint(currentPoint, EraserRadius);
                }
            }
            else
            {
                EraseStrokesAtPoint(currentPoint, EraserRadius);
            }

            _lastErasePoint = currentPoint;
            _hasLastErasePoint = true;
        }

        /// <summary>
        /// Xóa các nét vẽ/hình học cắt qua hoặc nằm trong bán kính tẩy
        /// </summary>
        private void EraseStrokesAtPoint(Point center, double radius)
        {
            // 1. Kiểm tra xóa trực tiếp nếu click trúng một Shape trên canvas (Rectangle, Ellipse, Line...)
            HitTestResult hitResult = VisualTreeHelper.HitTest(DrawingCanvas, center);
            if (hitResult?.VisualHit is Shape directHitShape && directHitShape != _eraserPreview && DrawingCanvas.Children.Contains(directHitShape))
            {
                if (!_currentEraseSessionElements.Contains(directHitShape))
                {
                    DrawingCanvas.Children.Remove(directHitShape);
                    if (directHitShape is Polyline polyDirect) _strokes.Remove(polyDirect);
                    _currentEraseSessionElements.Add(directHitShape);
                }
            }

            // 2. Kiểm tra va chạm với các nét vẽ Polyline
            var strokesToRemove = new List<Polyline>();
            foreach (var stroke in _strokes)
            {
                if (_currentEraseSessionElements.Contains(stroke)) continue;

                var points = stroke.Points;
                if (points == null || points.Count == 0) continue;

                double effectiveRadius = radius + (stroke.StrokeThickness / 2.0);
                double effectiveRadiusSq = effectiveRadius * effectiveRadius;
                bool hit = false;

                if (points.Count == 1)
                {
                    double d2 = (points[0].X - center.X) * (points[0].X - center.X) +
                                (points[0].Y - center.Y) * (points[0].Y - center.Y);
                    if (d2 <= effectiveRadiusSq) hit = true;
                }
                else
                {
                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        if (DistanceSquaredToSegment(center, points[i], points[i + 1]) <= effectiveRadiusSq)
                        {
                            hit = true;
                            break;
                        }
                    }
                }

                if (hit)
                {
                    strokesToRemove.Add(stroke);
                }
            }

            foreach (var stroke in strokesToRemove)
            {
                DrawingCanvas.Children.Remove(stroke);
                _strokes.Remove(stroke);
                _currentEraseSessionElements.Add(stroke);
            }
        }

        /// <summary>
        /// Kết thúc một phiên kéo xóa: ghi nhận Undo/Redo (Batch Action) và thông báo
        /// </summary>
        private void FinishEraseSession()
        {
            _isErasing = false;
            _hasLastErasePoint = false;

            if (_currentEraseSessionElements.Count == 0) return;

            if (_currentEraseSessionElements.Count == 1)
            {
                var elem = _currentEraseSessionElements[0];
                _undoRedoManager?.RecordRemoveAction(elem, DrawingCanvas, "Xóa nét (click)");
                ShowToast($"❌ Đã xóa 1 nét vẽ. Còn lại: {_strokes.Count}", "#D32F2F");
            }
            else
            {
                var batchActions = new List<Managers.UndoRedoAction>(_currentEraseSessionElements.Count);
                foreach (var elem in _currentEraseSessionElements)
                {
                    batchActions.Add(new Managers.UndoRedoAction
                    {
                        Type = Managers.ActionType.Remove,
                        Element = elem,
                        Parent = DrawingCanvas,
                        Description = "Xóa nét vẽ (kéo giữ)"
                    });
                }
                _undoRedoManager?.RecordBatchAction(batchActions, $"Xóa {_currentEraseSessionElements.Count} nét vẽ");
                ShowToast($"❌ Đã xóa {_currentEraseSessionElements.Count} nét vẽ. Còn lại: {_strokes.Count}", "#D32F2F");
            }

            _currentEraseSessionElements.Clear();
        }

        /// <summary>
        /// Tính khoảng cách bình phương từ một điểm P tới đoạn thẳng AB
        /// </summary>
        private static double DistanceSquaredToSegment(Point p, Point a, Point b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double lenSq = dx * dx + dy * dy;
            if (lenSq < 1e-6)
            {
                double px = p.X - a.X;
                double py = p.Y - a.Y;
                return px * px + py * py;
            }
            double t = Math.Max(0.0, Math.Min(1.0, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lenSq));
            double projX = a.X + t * dx;
            double projY = a.Y + t * dy;
            double distX = p.X - projX;
            double distY = p.Y - projY;
            return distX * distX + distY * distY;
        }

        /// <summary>
        /// Đảm bảo vòng tròn chỉ báo tẩy tồn tại trên canvas
        /// </summary>
        private void EnsureEraserPreview()
        {
            if (_eraserPreview == null || !DrawingCanvas.Children.Contains(_eraserPreview))
            {
                _eraserPreview = new Ellipse
                {
                    Width = EraserRadius * 2,
                    Height = EraserRadius * 2,
                    Stroke = new SolidColorBrush(Color.FromArgb(200, 244, 67, 54)), // #F44336
                    StrokeThickness = 1.5,
                    StrokeDashArray = new DoubleCollection { 3, 2 },
                    Fill = new SolidColorBrush(Color.FromArgb(35, 244, 67, 54)),
                    IsHitTestVisible = false,
                    Visibility = Visibility.Collapsed
                };
                DrawingCanvas.Children.Add(_eraserPreview);
                System.Windows.Controls.Canvas.SetZIndex(_eraserPreview, 10000);
            }
        }

        /// <summary>
        /// Cập nhật vị trí vòng tròn chỉ báo tẩy theo con trỏ chuột
        /// </summary>
        private void UpdateEraserPreview(Point pos)
        {
            if (!_isEraseByClickMode)
            {
                if (_eraserPreview != null) _eraserPreview.Visibility = Visibility.Collapsed;
                return;
            }

            EnsureEraserPreview();
            if (_eraserPreview != null)
            {
                System.Windows.Controls.Canvas.SetLeft(_eraserPreview, pos.X - EraserRadius);
                System.Windows.Controls.Canvas.SetTop(_eraserPreview, pos.Y - EraserRadius);
                if (_eraserPreview.Visibility != Visibility.Visible)
                {
                    _eraserPreview.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// Tạo con trỏ chuột hình Cục Tẩy (Eraser) chuyên nghiệp thay vì dấu '+' mặc định
        /// </summary>
        private static Cursor GetOrCreateEraserCursor()
        {
            if (_customEraserCursor != null) return _customEraserCursor;

            try
            {
                const int size = 32;
                using var bmp = new System.Drawing.Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.Clear(System.Drawing.Color.Transparent);

                    var bodyPoints = new System.Drawing.PointF[]
                    {
                        new System.Drawing.PointF(4, 26),
                        new System.Drawing.PointF(14, 26),
                        new System.Drawing.PointF(27, 8),
                        new System.Drawing.PointF(17, 8)
                    };

                    var sleevePoints = new System.Drawing.PointF[]
                    {
                        new System.Drawing.PointF(10, 18),
                        new System.Drawing.PointF(20, 18),
                        new System.Drawing.PointF(27, 8),
                        new System.Drawing.PointF(17, 8)
                    };

                    using var rubberBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 255, 107, 129));
                    using var sleeveBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 41, 121, 255));
                    using var outlinePen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(255, 33, 33, 33), 1.5f);
                    using var whitePen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(200, 255, 255, 255), 1.0f);

                    // Thân tẩy: màu hồng pastel
                    g.FillPolygon(rubberBrush, bodyPoints);
                    // Vỏ bọc: màu xanh dương
                    g.FillPolygon(sleeveBrush, sleevePoints);
                    // Đường viền phản chiếu ánh sáng
                    g.DrawLine(whitePen, 6, 24, 11, 17);
                    // Viền đen sắc nét
                    g.DrawPolygon(outlinePen, bodyPoints);
                    // Vạch ngăn giữa vỏ bọc và đầu tẩy
                    g.DrawLine(outlinePen, 10, 18, 20, 18);
                }

                IntPtr hIcon = bmp.GetHicon();
                if (hIcon != IntPtr.Zero)
                {
                    if (GetIconInfo(hIcon, out ICONINFO ii))
                    {
                        ii.fIcon = false; // false = cursor (không phải icon)
                        ii.xHotspot = 4;   // Hotspot ở ngay đầu mút tẩy góc dưới-trái
                        ii.yHotspot = 26;
                        IntPtr hCursor = CreateIconIndirect(ref ii);

                        if (ii.hbmColor != IntPtr.Zero) DeleteObject(ii.hbmColor);
                        if (ii.hbmMask != IntPtr.Zero) DeleteObject(ii.hbmMask);
                        DestroyIcon(hIcon);

                        if (hCursor != IntPtr.Zero)
                        {
                            var safeHandle = new Microsoft.Win32.SafeHandles.SafeFileHandle(hCursor, ownsHandle: true);
                            _customEraserCursor = System.Windows.Interop.CursorInteropHelper.Create(safeHandle);
                            return _customEraserCursor;
                        }
                    }
                    else
                    {
                        DestroyIcon(hIcon);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Không thể tạo con trỏ tẩy tùy biến: {ex.Message}");
            }

            return Cursors.Hand; // Dự phòng an toàn
        }

        /// <summary>
        /// Hi\u1ec3n th\u1ecb th\u00f4ng b\u00e1o t\u1ea1m th\u1eddi tr\u00ean overlay
        /// </summary>
        public void ShowToast(string message, string colorHex = "#333333")
        {
            try
            {
                var toast = new System.Windows.Controls.Border
                {
                    Background = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex)),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(16, 8, 16, 8),
                    Opacity = 0.92
                };

                var txt = new System.Windows.Controls.TextBlock
                {
                    Text = message,
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 15,
                    FontWeight = FontWeights.SemiBold
                };
                toast.Child = txt;

                // Đặt giữa-dưới màn hình
                double screenW = SystemParameters.PrimaryScreenWidth;
                double screenH = SystemParameters.PrimaryScreenHeight;
                System.Windows.Controls.Canvas.SetLeft(toast, screenW / 2 - 140);
                System.Windows.Controls.Canvas.SetTop(toast, screenH - 120);
                System.Windows.Controls.Canvas.SetZIndex(toast, 9999);

                DrawingCanvas.Children.Add(toast);

                // Tự xóa sau 2 giây
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2)
                };
                timer.Tick += (s, e) =>
                {
                    DrawingCanvas.Children.Remove(toast);
                    timer.Stop();
                };
                timer.Start();
            }
            catch { /* silent */ }
        }

        #endregion

        #region Undo/Redo

        public void Undo()
        {
            var action = _undoRedoManager?.Undo();
            if (action != null)
            {
                ExecuteUndoAction(action);
                System.Diagnostics.Debug.WriteLine($"↶ Undo: {action.Description}");
            }
        }

        public void Redo()
        {
            var action = _undoRedoManager?.Redo();
            if (action != null)
            {
                ExecuteRedoAction(action);
                System.Diagnostics.Debug.WriteLine($"↷ Redo: {action.Description}");
            }
        }

        /// <summary>
        /// Capture the overlay with annotations as an image
        /// </summary>
        public System.Windows.Media.Imaging.BitmapSource? CaptureToImage()
        {
            try
            {
                // Get the size of the overlay
                double width = this.ActualWidth;
                double height = this.ActualHeight;

                if (width == 0 || height == 0)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Overlay has no size, cannot capture");
                    return null;
                }

                // Create a render target bitmap
                var renderBitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)width,
                    (int)height,
                    96, // DPI X
                    96, // DPI Y
                    PixelFormats.Pbgra32
                );

                // Render the overlay to the bitmap
                renderBitmap.Render(this);

                System.Diagnostics.Debug.WriteLine($"📷 Captured overlay: {width}x{height}");
                return renderBitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Capture error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Insert an image onto the canvas at specified position
        /// </summary>
        public void InsertImage(System.Windows.Media.Imaging.BitmapSource image, Point position)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"📷 InsertImage called: {image.PixelWidth}x{image.PixelHeight} at ({position.X:F0}, {position.Y:F0})");
                
                var imageControl = new System.Windows.Controls.Image
                {
                    Source = image,
                    Width = image.PixelWidth,
                    Height = image.PixelHeight,
                    Stretch = System.Windows.Media.Stretch.Fill,
                    Opacity = 1.0,
                    Visibility = Visibility.Visible
                };
                
                // Add to canvas
                DrawingCanvas.Children.Add(imageControl);
                System.Windows.Controls.Canvas.SetLeft(imageControl, position.X);
                System.Windows.Controls.Canvas.SetTop(imageControl, position.Y);
                System.Windows.Controls.Canvas.SetZIndex(imageControl, 100); // Ensure on top
                
                // Record for undo
                _undoRedoManager?.RecordAddAction(imageControl, "Insert image");
                
                System.Diagnostics.Debug.WriteLine($"✅ Image inserted successfully! Canvas children count: {DrawingCanvas.Children.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Insert image error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
        }

        private void ExecuteUndoAction(Managers.UndoRedoAction action)
        {
            if (action.Type == Managers.ActionType.Batch)
            {
                for (int i = action.BatchActions.Count - 1; i >= 0; i--)
                {
                    ExecuteUndoAction(action.BatchActions[i]);
                }
                return;
            }

            if (action.Element != null)
            {
                if (action.Type == Managers.ActionType.Add)
                {
                    // Undo "Add" = Xóa phần tử
                    DrawingCanvas.Children.Remove(action.Element);
                    if (action.Element is Polyline stroke)
                        _strokes.Remove(stroke);
                }
                else if (action.Type == Managers.ActionType.Remove)
                {
                    // Undo "Remove" = Khôi phục phần tử
                    if (!DrawingCanvas.Children.Contains(action.Element))
                        DrawingCanvas.Children.Add(action.Element);
                    if (action.Element is Polyline stroke && !_strokes.Contains(stroke))
                        _strokes.Add(stroke);
                }
            }
        }

        private void ExecuteRedoAction(Managers.UndoRedoAction action)
        {
            if (action.Type == Managers.ActionType.Batch)
            {
                foreach (var subAction in action.BatchActions)
                {
                    ExecuteRedoAction(subAction);
                }
                return;
            }

            if (action.Element != null)
            {
                if (action.Type == Managers.ActionType.Add)
                {
                    // Redo "Add" = Thêm lại phần tử
                    if (!DrawingCanvas.Children.Contains(action.Element))
                        DrawingCanvas.Children.Add(action.Element);
                    if (action.Element is Polyline stroke && !_strokes.Contains(stroke))
                        _strokes.Add(stroke);
                }
                else if (action.Type == Managers.ActionType.Remove)
                {
                    // Redo "Remove" = Xóa lại phần tử
                    DrawingCanvas.Children.Remove(action.Element);
                    if (action.Element is Polyline stroke)
                        _strokes.Remove(stroke);
                }
            }
        }

        public bool CanUndo => _undoRedoManager?.CanUndo ?? false;
        public bool CanRedo => _undoRedoManager?.CanRedo ?? false;
        public Managers.UndoRedoManager? UndoRedoManager => _undoRedoManager;

        #endregion

        #region Statistics

        public int GetStrokeCount()
        {
            return _strokes.Count;
        }

        public string GetStatistics()
        {
            return $"Mode: {(_isPenMode ? "Pen" : "Mouse")}, " +
                   $"Tool: {_currentTool}, " +
                   $"Strokes: {_strokes.Count}";
        }

        #endregion
    }

    /// <summary>
    /// Annotation tools
    /// </summary>
    public enum AnnotationTool
    {
        Pen,
        Highlighter,
        Eraser,
        Line,
        Rectangle,
        Circle
    }
    
    /// <summary>
    /// Event args for toolbar repositioning
    /// </summary>
    public class ToolbarSideEventArgs : EventArgs
    {
        public bool IsLeftSide { get; }
        
        public ToolbarSideEventArgs(bool isLeftSide)
        {
            IsLeftSide = isLeftSide;
        }
    }
}
