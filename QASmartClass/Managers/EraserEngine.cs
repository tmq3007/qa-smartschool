using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using QASmartTouch.Helpers;
using Serilog;

namespace QASmartTouch.Managers
{
    /// <summary>
    /// Eraser modes available
    /// </summary>
    public enum EraserMode
    {
        Stroke,     // Erase entire stroke on click
        Drag,       // Erase by dragging over area
        ClearAll    // Clear all objects
    }
    
    /// <summary>
    /// Handles eraser operations (stroke, drag, clear all)
    /// Manages eraser state, modes, and object removal
    /// </summary>
    public class EraserEngine
    {
        #region Constants
        
        /// <summary>
        /// Minimum eraser size
        /// </summary>
        public const int MIN_ERASER_SIZE = 10;
        
        /// <summary>
        /// Maximum eraser size
        /// </summary>
        public const int MAX_ERASER_SIZE = 100;
        
        /// <summary>
        /// Default eraser size
        /// </summary>
        public const int DEFAULT_ERASER_SIZE = 20;

        /// <summary>
        /// [LOI_VID_51] Hit test margin mặc định (px) — DPI 96.
        /// Sẽ được nhân với hệ số DPI thực tế để đảm bảo consistent trên 4K 150%.
        /// Theo phản biện ThS. Vũ Hoàng Anh: scale = 3.0 * (dpi/96.0)
        /// </summary>
        private const double BASE_HIT_TEST_MARGIN = 3.0;

        /// <summary>
        /// [LOI_VID_51] Ngưỡng tối thiểu kích thước element để không bị coi là ghost artifact (px).
        /// Element < 3x3px sẽ bị loại bỏ sau khi tẩy.
        /// </summary>
        private const double GHOST_ARTIFACT_THRESHOLD = 3.0;
        
        #endregion
        
        #region Fields
        
        private readonly Canvas _canvas;
        private bool _isEraserActive;
        private EraserMode _eraserMode;
        private int _eraserSize;
        private Ellipse? _eraserPreview;
        
        // BUG-1601: Fill bán trong suốt cho Shape rỗng — alpha=1/255, mắt thường không nhìn thấy
        // nhưng đủ để WPF HitTest phát hiện khi ngón tay chạm vào vùng lòng hình vẽ.
        private static readonly Brush _invisibleFill = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
        
        // BUG-1601: Lưu trữ danh sách Shape đã bị gán Fill ẩn kèm Fill gốc để khôi phục sau.
        private readonly List<(Shape shape, Brush? originalFill)> _modifiedShapes = new();
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Gets whether eraser is currently active
        /// </summary>
        public bool IsEraserActive => _isEraserActive;
        
        /// <summary>
        /// Gets the current eraser mode
        /// </summary>
        public EraserMode EraserMode => _eraserMode;
        
        /// <summary>
        /// Gets the current eraser size
        /// </summary>
        public int EraserSize => _eraserSize;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when an object is erased
        /// </summary>
        public event EventHandler<ObjectErasedEventArgs>? ObjectErased;
        
        /// <summary>
        /// Fired when all objects are cleared
        /// </summary>
        public event EventHandler? AllCleared;
        
        /// <summary>
        /// Fired when eraser settings are changed
        /// </summary>
        public event EventHandler<EraserSettingsChangedEventArgs>? SettingsChanged;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of EraserEngine
        /// </summary>
        /// <param name="canvas">The canvas to erase from</param>
        public EraserEngine(Canvas canvas)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            _eraserMode = EraserMode.Stroke;
            _eraserSize = DEFAULT_ERASER_SIZE;
            _isEraserActive = false;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Enables the eraser with specified mode
        /// </summary>
        /// <param name="mode">The eraser mode to use</param>
        public void EnableEraser(EraserMode mode)
        {
            _isEraserActive = true;
            _eraserMode = mode;
            
            // BUG-1601: Gán Fill ẩn cho Shape rỗng để HitTest phát hiện được
            if (mode == EraserMode.Stroke || mode == EraserMode.Drag)
            {
                SetInvisibleFillForEmptyShapes();
            }
            
            System.Diagnostics.Debug.WriteLine($"✅ Eraser enabled: Mode={mode}");
        }
        
        /// <summary>
        /// Disables the eraser
        /// </summary>
        public void DisableEraser()
        {
            _isEraserActive = false;
            HideEraserPreview();
            
            // BUG-1601: Khôi phục Fill gốc cho các Shape đã bị gán Fill ẩn
            RestoreOriginalFills();
            
            System.Diagnostics.Debug.WriteLine($"❌ Eraser disabled");
        }
        
        /// <summary>
        /// Erases a single stroke/object
        /// </summary>
        /// <param name="element">The element to erase</param>
        /// <returns>True if erased successfully, false otherwise</returns>
        public bool EraseByStroke(UIElement element)
        {
            if (element == null || !_canvas.Children.Contains(element))
                return false;
            
            // BUG-1601: Khôi phục Fill gốc cho Shape trước khi xóa,
            // để khi Undo add lại, Shape hiển thị đúng Fill gốc (null/Transparent)
            // thay vì Fill ẩn alpha=1.
            RestoreOriginalFillForElement(element);
            
            _canvas.Children.Remove(element);
            
            System.Diagnostics.Debug.WriteLine($"🗑️ Erased element: {element.GetType().Name}");
            
            // Fire event
            ObjectErased?.Invoke(this, new ObjectErasedEventArgs(element));
            
            return true;
        }
        
        /// <summary>
        /// BUG-1601: Trả về eraser preview element để CanvasEventHandlers
        /// loại trừ khỏi HitTest (tránh tẩy nhầm chính preview).
        /// </summary>
        public UIElement? GetEraserPreview() => _eraserPreview;
        
        /// <summary>
        /// [LOI_VID_51] Erases all objects within a rectangular area.
        /// Đã nâng cấp:
        /// - DPI-aware hit test margin
        /// - Lọc bỏ micro-elements (ghost artifacts) < 3x3px
        /// - Batch remove + InvalidateVisual có điều kiện
        /// - Stopwatch performance logging
        /// </summary>
        /// <param name="area">The area to erase within</param>
        /// <returns>List of erased elements</returns>
        public List<UIElement> EraseByDrag(Rect area)
        {
            var sw = Stopwatch.StartNew();
            var erasedElements = new List<UIElement>();
            var elementsToRemove = new List<UIElement>();
            var ghostArtifacts = new List<UIElement>();

            // [LOI_VID_51] DPI-aware hit test margin
            double dpiScale = GetDpiScale();
            double hitMargin = BASE_HIT_TEST_MARGIN * dpiScale;

            // Mở rộng vùng tẩy thêm hit margin
            var expandedArea = new Rect(
                area.X - hitMargin,
                area.Y - hitMargin,
                area.Width + hitMargin * 2,
                area.Height + hitMargin * 2);
            
            foreach (UIElement child in _canvas.Children)
            {
                // Skip system UI elements
                if (IsSystemElement(child))
                    continue;
                
                // Check if element intersects with expanded erase area
                if (ElementIntersectsArea(child, expandedArea))
                {
                    elementsToRemove.Add(child);
                }
            }
            
            // [LOI_VID_51] Batch remove elements
            foreach (var element in elementsToRemove)
            {
                // BUG-1602: Khôi phục Fill gốc trước khi xóa (đồng bộ với BUG-1601)
                RestoreOriginalFillForElement(element);
                
                _canvas.Children.Remove(element);
                erasedElements.Add(element);
                
                // Fire event for each erased element
                ObjectErased?.Invoke(this, new ObjectErasedEventArgs(element));
            }

            // [LOI_VID_51] Quét và loại bỏ ghost artifacts (micro-elements < 3x3px)
            // sau khi tẩy xong — những mảnh vụn nhỏ còn sót lại
            foreach (UIElement child in _canvas.Children)
            {
                if (IsSystemElement(child)) continue;

                var bounds = GetElementBounds(child);
                if (bounds.Width < GHOST_ARTIFACT_THRESHOLD 
                    && bounds.Height < GHOST_ARTIFACT_THRESHOLD
                    && bounds.Width > 0 && bounds.Height > 0)
                {
                    // Nằm trong hoặc gần vùng tẩy → ghost artifact
                    if (expandedArea.IntersectsWith(bounds))
                    {
                        ghostArtifacts.Add(child);
                    }
                }
            }

            // Xóa ghost artifacts
            foreach (var ghost in ghostArtifacts)
            {
                RestoreOriginalFillForElement(ghost);
                _canvas.Children.Remove(ghost);
                erasedElements.Add(ghost);
            }

            // [LOI_VID_51] InvalidateVisual chỉ khi có ghost artifacts được xóa
            // (theo phản biện ThS. Phạm Quốc Đạt: không gọi mỗi lần tẩy)
            if (ghostArtifacts.Count > 0)
            {
                _canvas.InvalidateVisual();
                Log.Debug("[LOI_VID_51] Cleaned {Count} ghost artifacts, InvalidateVisual called",
                    ghostArtifacts.Count);
            }
            
            sw.Stop();
            if (erasedElements.Count > 0)
            {
                Log.Information(
                    "[LOI_VID_51] EraseByDrag: {Count} elements (incl. {Ghosts} ghosts) in {Ms}ms " +
                    "(area={W:F0}x{H:F0}, dpi={Dpi:F1}, margin={Margin:F1}px)",
                    erasedElements.Count, ghostArtifacts.Count, sw.ElapsedMilliseconds,
                    area.Width, area.Height, dpiScale, hitMargin);
            }
            
            return erasedElements;
        }
        
        /// <summary>
        /// Clears all objects from the canvas
        /// </summary>
        /// <returns>List of cleared elements</returns>
        public List<UIElement> ClearAll()
        {
            var clearedElements = new List<UIElement>();
            var elementsToRemove = new List<UIElement>();
            
            foreach (UIElement child in _canvas.Children)
            {
                // Skip system UI elements
                if (IsSystemElement(child))
                    continue;
                
                elementsToRemove.Add(child);
            }
            
            // Remove elements
            foreach (var element in elementsToRemove)
            {
                _canvas.Children.Remove(element);
                clearedElements.Add(element);
            }
            
            System.Diagnostics.Debug.WriteLine($"🗑️ Cleared all: {clearedElements.Count} elements");
            
            // Fire event
            AllCleared?.Invoke(this, EventArgs.Empty);
            
            return clearedElements;
        }
        
        /// <summary>
        /// Sets the eraser size
        /// </summary>
        /// <param name="size">The eraser size (10-100)</param>
        public void SetEraserSize(int size)
        {
            size = Math.Clamp(size, MIN_ERASER_SIZE, MAX_ERASER_SIZE);
            
            if (_eraserSize == size)
                return;
            
            var oldSize = _eraserSize;
            _eraserSize = size;
            
            // Update preview if visible
            if (_eraserPreview != null)
            {
                _eraserPreview.Width = size;
                _eraserPreview.Height = size;
            }
            
            System.Diagnostics.Debug.WriteLine($"✅ Eraser size changed: {oldSize} → {size}");
            
            // Fire event
            SettingsChanged?.Invoke(this, new EraserSettingsChangedEventArgs(_eraserMode, size));
        }
        
        /// <summary>
        /// Shows the eraser cursor preview at specified position
        /// </summary>
        /// <param name="position">The position to show the preview</param>
        public void ShowEraserPreview(Point position)
        {
            if (_eraserPreview == null)
            {
                CreateEraserPreview();
            }
            
            if (_eraserPreview != null)
            {
                Canvas.SetLeft(_eraserPreview, position.X - _eraserSize / 2);
                Canvas.SetTop(_eraserPreview, position.Y - _eraserSize / 2);
                _eraserPreview.Visibility = Visibility.Visible;
            }
        }
        
        /// <summary>
        /// Hides the eraser cursor preview
        /// </summary>
        public void HideEraserPreview()
        {
            if (_eraserPreview != null)
            {
                _eraserPreview.Visibility = Visibility.Collapsed;
            }
        }
        
        /// <summary>
        /// Updates the eraser preview position
        /// </summary>
        /// <param name="position">The new position</param>
        public void UpdateEraserPreview(Point position)
        {
            if (_eraserPreview == null)
            {
                CreateEraserPreview();
            }
            if (_eraserPreview != null)
            {
                Canvas.SetLeft(_eraserPreview, position.X - _eraserSize / 2.0);
                Canvas.SetTop(_eraserPreview, position.Y - _eraserSize / 2.0);
                if (_eraserPreview.Visibility != Visibility.Visible)
                {
                    _eraserPreview.Visibility = Visibility.Visible;
                }
            }
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// BUG-1601: Đặt Fill bán trong suốt cho các Shape rỗng (Fill=null hoặc Transparent)
        /// để WPF HitTest có thể phát hiện khi ngón tay chạm vào vùng lòng của hình vẽ.
        /// Fill này có alpha=1/255 (0.39% opacity), mắt thường hoàn toàn không nhìn thấy
        /// kể cả trên màn hình IFP 500 nits.
        /// </summary>
        private void SetInvisibleFillForEmptyShapes()
        {
            _modifiedShapes.Clear();
            
            foreach (UIElement child in _canvas.Children)
            {
                if (child is Shape shape && shape != _eraserPreview)
                {
                    bool isEmpty = shape.Fill == null
                                || (shape.Fill is SolidColorBrush scb && scb.Color.A == 0);
                    
                    if (isEmpty)
                    {
                        _modifiedShapes.Add((shape, shape.Fill));
                        shape.Fill = _invisibleFill;
                    }
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"🎨 BUG-1601: Set invisible fill for {_modifiedShapes.Count} empty shapes");
        }
        
        /// <summary>
        /// BUG-1601: Khôi phục Fill gốc cho tất cả Shape đã bị gán Fill ẩn.
        /// Được gọi khi: (a) tắt Eraser, (b) trước Save/Export canvas.
        /// </summary>
        public void RestoreOriginalFills()
        {
            foreach (var (shape, originalFill) in _modifiedShapes)
            {
                // Chỉ khôi phục cho Shape chưa bị xóa khỏi Canvas
                if (_canvas.Children.Contains(shape))
                {
                    shape.Fill = originalFill;
                }
            }
            
            var count = _modifiedShapes.Count;
            _modifiedShapes.Clear();
            
            if (count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"🎨 BUG-1601: Restored original fill for {count} shapes");
            }
        }
        
        /// <summary>
        /// BUG-1601: Khôi phục Fill gốc cho một Shape cụ thể trước khi xóa nó.
        /// Đảm bảo khi Undo add lại Shape, Fill hiển thị đúng (null/Transparent)
        /// thay vì Fill ẩn alpha=1.
        /// </summary>
        private void RestoreOriginalFillForElement(UIElement element)
        {
            if (element is Shape shape)
            {
                for (int i = _modifiedShapes.Count - 1; i >= 0; i--)
                {
                    if (_modifiedShapes[i].shape == shape)
                    {
                        shape.Fill = _modifiedShapes[i].originalFill;
                        _modifiedShapes.RemoveAt(i);
                        System.Diagnostics.Debug.WriteLine(
                            $"🎨 BUG-1601: Restored fill for {shape.GetType().Name} before erase");
                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// Creates the eraser cursor preview
        /// </summary>
        private void CreateEraserPreview()
        {
            _eraserPreview = new Ellipse
            {
                Width = _eraserSize,
                Height = _eraserSize,
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(50, 255, 0, 0)),
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed
            };
            
            _canvas.Children.Add(_eraserPreview);
            Canvas.SetZIndex(_eraserPreview, 10000); // Always on top
        }
        
        /// <summary>
        /// Checks if an element is a system UI element (should not be erased)
        /// </summary>
        /// <param name="element">The element to check</param>
        /// <returns>True if system element, false otherwise</returns>
        private bool IsSystemElement(UIElement element)
        {
            // Skip eraser preview
            if (element == _eraserPreview)
                return true;
            
            // BUG-1602: Skip drag erase preview rectangle
            // Kiểm tra bằng tag name để không cần tham chiếu trực tiếp từ CanvasEventHandlers
            if (element is Rectangle rect && rect.IsHitTestVisible == false 
                && rect.StrokeDashArray != null && rect.StrokeDashArray.Count > 0)
            {
                // Khả năng cao là drag erase preview (nét đứt, IsHitTestVisible=false)
                return true;
            }
            
            // ✅ QC_4.2_TABLE_ERASER_PROTECT (T13): Bảo vệ Table container khỏi bị Tẩy xóa
            // Tẩy chỉ xóa nét mực (Polyline) bên trong ô, KHÔNG xóa khung/viền Bảng
            if (IsTableContainer(element))
                return true;
            
            return false;
        }

        /// <summary>
        /// ✅ QC_4.2_WIDGET_ERASER_PROTECT (T13+TB-5): Kiểm tra element có thuộc Widget container hay không.
        /// Bảng + Hộp Văn Bản được bảo vệ khỏi Eraser — GV phải dùng nút ❌ hoặc Selection+Delete để xóa.
        /// </summary>
        /// <param name="element">Phần tử cần kiểm tra</param>
        /// <returns>true nếu element là hoặc thuộc Widget container được bảo vệ</returns>
        private bool IsTableContainer(UIElement element)
        {
            // Kiểm tra trực tiếp: Tag = "TableContainer" hoặc "TextBoxContainer"
            if (element is FrameworkElement fe)
            {
                // ✅ QC_4.2_WIDGET_ERASER_PROTECT: Bảo vệ TẤT CẢ Canvas Widget có DragHandle
                if (fe.Tag is string tag && (tag == "TableContainer" || tag == "TextBoxContainer"))
                    return true;
            }

            // Kiểm tra parent: element con bên trong Widget (Border, Grid, TextBlock...)
            if (element is System.Windows.DependencyObject depObj)
            {
                var parent = System.Windows.Media.VisualTreeHelper.GetParent(depObj);
                while (parent != null && !(parent is Canvas))
                {
                    if (parent is FrameworkElement parentFe 
                        && parentFe.Tag is string parentTag 
                        && (parentTag == "TableContainer" || parentTag == "TextBoxContainer"))
                    {
                        return true;
                    }
                    parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
                }
            }

            return false;
        }
        
        /// <summary>
        /// Checks if an element intersects with an area
        /// </summary>
        /// <param name="element">The element to check</param>
        /// <param name="area">The area to check intersection with</param>
        /// <returns>True if intersects, false otherwise</returns>
        private bool ElementIntersectsArea(UIElement element, Rect area)
        {
            // Get element bounds
            var elementBounds = GetElementBounds(element);
            
            // Check intersection
            return area.IntersectsWith(elementBounds);
        }
        
        /// <summary>
        /// IMP-1604: Tính bounds chính xác sử dụng BoundsHelper utility.
        /// Thay thế logic inline từ BUG-1602 bằng hàm tổng quát.
        /// </summary>
        /// <param name="element">The element to get bounds for</param>
        /// <returns>The bounding rectangle in Canvas coordinates</returns>
        private Rect GetElementBounds(UIElement element)
        {
            return BoundsHelper.GetAbsoluteBounds(element, _canvas);
        }
        
        #endregion

        // ═══════════════════════════════════════════════════════
        //  [LOI_VID_51] DPI UTILITIES
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// [LOI_VID_51] Lấy hệ số DPI scale hiện tại.
        /// DPI 96 = 1.0, DPI 144 (150%) = 1.5, DPI 192 (200%) = 2.0
        /// </summary>
        private double GetDpiScale()
        {
            try
            {
                var source = PresentationSource.FromVisual(_canvas);
                if (source?.CompositionTarget != null)
                {
                    return source.CompositionTarget.TransformToDevice.M11;
                }
            }
            catch { /* fallback */ }
            return 1.0; // Fallback: DPI 96 (100%)
        }
    }
    
    #region Event Args
    
    /// <summary>
    /// Event arguments for object erased events
    /// </summary>
    public class ObjectErasedEventArgs : EventArgs
    {
        public UIElement Element { get; }
        
        public ObjectErasedEventArgs(UIElement element)
        {
            Element = element;
        }
    }
    
    /// <summary>
    /// Event arguments for eraser settings changed events
    /// </summary>
    public class EraserSettingsChangedEventArgs : EventArgs
    {
        public EraserMode Mode { get; }
        public int Size { get; }
        
        public EraserSettingsChangedEventArgs(EraserMode mode, int size)
        {
            Mode = mode;
            Size = size;
        }
    }
    
    #endregion
}
