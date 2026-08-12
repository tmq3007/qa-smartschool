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
        #region Win32 API for Click-Through

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        #endregion

        private bool _isPenMode = false;
        private bool _isDrawing = false;
        private bool _isEraseByClickMode = false;  // Chế độ xóa từng nét bằng click
        private Polyline? _currentStroke;
        private List<Polyline> _strokes = new List<Polyline>();
        private Managers.UndoRedoManager? _undoRedoManager;
        
        // Drawing properties
        private Color _currentColor = Colors.Red;
        private double _currentThickness = 3;
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
                    break;
                case AnnotationTool.Eraser:
                    newCursor = Cursors.Cross;
                    break;
                default:
                    newCursor = Cursors.Cross;
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
                    Width = screenshot.PixelWidth,
                    Height = screenshot.PixelHeight,
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

            // Chế độ xóa từng nét bằng click
            if (_isEraseByClickMode)
            {
                var point = e.GetPosition(DrawingCanvas);
                EraseStrokeAtPoint(point);
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
            if (!_isDrawing) return;

            var point = e.GetPosition(DrawingCanvas);
            ContinueDrawing(point);
        }

        private void DrawingCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.StylusDevice != null) return; // Prevent duplicate handling from touch/stylus
            if (!_isDrawing) return;

            EndDrawing();
        }

        #endregion

        #region Stylus Events

        private void DrawingCanvas_StylusDown(object sender, StylusDownEventArgs e)
        {
            // Chế độ xóa từng nét bằng click
            if (_isEraseByClickMode)
            {
                var point = e.GetPosition(DrawingCanvas);
                EraseStrokeAtPoint(point);
                return;
            }

            if (!_isPenMode || _currentTool == AnnotationTool.Eraser) return;

            _isDrawing = true;
            var pt = e.GetPosition(DrawingCanvas);
            StartDrawing(pt);
        }

        private void DrawingCanvas_StylusMove(object sender, StylusEventArgs e)
        {
            if (!_isDrawing) return;

            var point = e.GetPosition(DrawingCanvas);
            ContinueDrawing(point);
        }

        private void DrawingCanvas_StylusUp(object sender, StylusEventArgs e)
        {
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
            DrawingCanvas.Children.Add(_currentStroke);
            _strokes.Add(_currentStroke);

            System.Diagnostics.Debug.WriteLine($"Started drawing at ({point.X:F0}, {point.Y:F0})");
        }

        private void ContinueDrawing(Point point)
        {
            if (_currentStroke != null)
            {
                _currentStroke.Points.Add(point);
            }
        }

        private void EndDrawing()
        {
            if (_currentStroke != null)
            {
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
        /// Bật/tắt chế độ xóa từng nét bằng click (như "Xóa từng nét" trong công cụ Tẩy)
        /// </summary>
        public void ToggleEraseByClickMode()
        {
            _isEraseByClickMode = !_isEraseByClickMode;

            if (_isEraseByClickMode)
            {
                // Vào chế độ xóa: chỉnh con trỏ và bật click-capture
                SetClickThrough(false);  // Phải nhận click
                this.Cursor = Cursors.Cross;
                this.ForceCursor = true;
                DrawingCanvas.Cursor = Cursors.Cross;
                DrawingCanvas.ForceCursor = true;
                ShowToast("🧹 Chế độ Xóa Nét – Click vào nét để xóa | Nhấn lại để thoát", "#5C6BC0");
            }
            else
            {
                // Thoát chế độ xóa: khôi phục trạng thái cũ
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
        /// Xóa nét vẽ gần nhất tại điểm click (trong chế độ Erase-by-Click)
        /// </summary>
        private void EraseStrokeAtPoint(Point clickPoint)
        {
            if (_strokes.Count == 0)
            {
                ShowToast("🗨️ Không có nét vẽ nào!", "#E65100");
                return;
            }

            // Tìm nét gần điểm click nhất (trong khoảng 30px)
            Polyline? nearest = null;
            double minDist = 30.0;  // ngưỡng tối đa

            foreach (var stroke in _strokes)
            {
                foreach (var pt in stroke.Points)
                {
                    double d = Math.Sqrt(Math.Pow(pt.X - clickPoint.X, 2) + Math.Pow(pt.Y - clickPoint.Y, 2));
                    if (d < minDist)
                    {
                        minDist = d;
                        nearest = stroke;
                    }
                }
            }

            if (nearest == null)
            {
                // Không có nét nào gần, thử tăng ngưỡng lên 60px
                minDist = 60.0;
                foreach (var stroke in _strokes)
                {
                    foreach (var pt in stroke.Points)
                    {
                        double d = Math.Sqrt(Math.Pow(pt.X - clickPoint.X, 2) + Math.Pow(pt.Y - clickPoint.Y, 2));
                        if (d < minDist)
                        {
                            minDist = d;
                            nearest = stroke;
                        }
                    }
                }
            }

            if (nearest != null)
            {
                _undoRedoManager?.RecordRemoveAction(nearest, DrawingCanvas, "Xóa nét (click)");
                DrawingCanvas.Children.Remove(nearest);
                _strokes.Remove(nearest);
                ShowToast($"❌ Đã xóa 1 nét. Còn lại: {_strokes.Count}", "#D32F2F");
            }
            else
            {
                ShowToast("⚠️ Không tìm thấy nét vẽ tại đây", "#F57C00");
            }
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
