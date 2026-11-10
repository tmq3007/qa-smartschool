using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QASmartTouch.Forms
{
    /// <summary>
    /// Partial class extension for Form2_MainDashboard
    /// Contains methods for adding interactive images to canvas
    /// </summary>
    public partial class Form2_MainDashboard
    {
        /// <summary>
        /// Add captured image from Window Mode area selection
        /// Called by WindowModeController after area capture
        /// </summary>
        /// <param name="imageSource">The captured BitmapSource</param>
        public void AddCapturedImage(BitmapSource imageSource)
        {
            System.Diagnostics.Debug.WriteLine("📋 AddCapturedImage called from WindowModeController");
            
            // Use existing AddImageToCanvas method
            AddImageToCanvas(imageSource);
            
            System.Diagnostics.Debug.WriteLine("✅ Image added to MainDashboard canvas");
        }
        
        /// <summary>
        /// Add captured screenshot image to canvas with interactive features
        /// Features: Drag to move, Zoom with mouse wheel, Right-click to delete
        /// </summary>
        /// <param name="imageSource">The BitmapSource to add to canvas</param>
        public void AddImageToCanvas(BitmapSource imageSource)
        {
            if (imageSource == null) return;

            // Create container Grid for image + action panel
            var container = new Grid
            {
                Width = Math.Min(imageSource.PixelWidth, 800),
                Height = Math.Min(imageSource.PixelHeight, 600)
            };

            // Create Image element
            var image = new Image
            {
                Source = imageSource,
                Stretch = System.Windows.Media.Stretch.Uniform,
                Cursor = Cursors.Hand,
                Tag = "Screenshot"
            };
            
            // ✨ NEW: Wrap image in border (border will zoom with image)
            var imageBorder = new Border
            {
                Child = image,
                BorderBrush = null,
                BorderThickness = new Thickness(0)
            };
            
            // Add image border to container (this will be zoomed)
            container.Children.Add(imageBorder);

            // Set initial position (center of canvas)
            double x = (MainInteractiveBoard.ActualWidth - container.Width) / 2;
            double y = (MainInteractiveBoard.ActualHeight - container.Height) / 2;
            Canvas.SetLeft(container, Math.Max(0, x));
            Canvas.SetTop(container, Math.Max(0, y));

            // === DRAG TO MOVE ===
            Point? dragStart = null;
            Point? originalPosition = null;

            container.MouseLeftButtonDown += (s, e) =>
            {
                // Do not drag image when in Pen, Eraser, or Shape Drawing mode
                if (_drawingEnabled || _eraserEnabled || _shapeDrawingEnabled) return;

                // Only process if not clicking on a button or its children
                if (QASmartTouch.Utilities.InputValidationHelper.IsEventFromInteractiveControl(e.OriginalSource, container)) return;
                
                // Show this image's border
                imageBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // #2196F3
                imageBorder.BorderThickness = new Thickness(2);
                
                // Hide all other images' borders
                HideOtherActionPanels(container);
                
                // Start dragging
                dragStart = e.GetPosition(MainInteractiveBoard);
                originalPosition = new Point(Canvas.GetLeft(container), Canvas.GetTop(container));
                container.CaptureMouse();
                Panel.SetZIndex(container, GetMaxZIndex() + 1);
                
                e.Handled = true;
            };

            container.MouseMove += (s, e) =>
            {
                if (dragStart.HasValue && container.IsMouseCaptured)
                {
                    Point current = e.GetPosition(MainInteractiveBoard);
                    Canvas.SetLeft(container, originalPosition.Value.X + (current.X - dragStart.Value.X));
                    Canvas.SetTop(container, originalPosition.Value.Y + (current.Y - dragStart.Value.Y));
                }
            };

            container.MouseLeftButtonUp += (s, e) =>
            {
                if (dragStart.HasValue)
                {
                    container.ReleaseMouseCapture();
                    dragStart = null;

                    // ✅ SYNC-FIX: Cập nhật vị trí mới vào SelectableObject & Rebuild QuadTree
                    try
                    {
                        double finalLeft = Canvas.GetLeft(container);
                        double finalTop = Canvas.GetTop(container);
                        if (double.IsNaN(finalLeft)) finalLeft = 0;
                        if (double.IsNaN(finalTop)) finalTop = 0;

                        var obj = _selectionManager?.GetAllObjects().FirstOrDefault(o => o.Element == container);
                        if (obj != null)
                        {
                            obj.Position = new Point(finalLeft, finalTop);
                            obj.UpdateBounds();
                            _selectionManager?.RebuildQuadTree();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Error updating image bounds on drag end: {ex.Message}");
                    }
                }
            };

            // === ZOOM WITH MOUSE WHEEL ===
            imageBorder.MouseWheel += (s, e) =>
            {
                // ✨ FIX: Zoom imageBorder (includes border + image)
                var transform = imageBorder.RenderTransform as ScaleTransform ?? new ScaleTransform(1, 1);
                imageBorder.RenderTransform = transform;
                imageBorder.RenderTransformOrigin = new Point(0.5, 0.5);

                transform.ScaleX *= e.Delta > 0 ? 1.1 : 0.9;
                transform.ScaleY *= e.Delta > 0 ? 1.1 : 0.9;
                transform.ScaleX = Math.Max(0.1, Math.Min(5.0, transform.ScaleX));
                transform.ScaleY = Math.Max(0.1, Math.Min(5.0, transform.ScaleY));
                e.Handled = true;
            };
            
            // === HIDE BORDER WHEN CLICKING OUTSIDE (Memory leak free) ===
            MouseButtonEventHandler clickOutsideHandler = null!;
            clickOutsideHandler = (s, e) =>
            {
                // If click is not on this container, hide its border
                if (!IsMouseOverElement(container, e.GetPosition(MainInteractiveBoard)))
                {
                    imageBorder.BorderBrush = null;
                    imageBorder.BorderThickness = new Thickness(0);
                }
            };
            MainInteractiveBoard.MouseLeftButtonDown += clickOutsideHandler;

            container.Unloaded += (s, e) =>
            {
                MainInteractiveBoard.MouseLeftButtonDown -= clickOutsideHandler;
            };

            // === ADD TO CANVAS (QC_4.2_MILESTONE_ZINDEX) ===
            int mediaZ = AllocateMediaWidgetZIndex();
            Panel.SetZIndex(container, mediaZ);
            MainInteractiveBoard.Children.Add(container);
            try { RegisterNewObjectWithSelectionManager(container); } catch { }
            try { RecordAddAction(container, "Thêm ảnh chụp màn hình"); } catch { }
        }

        /// <summary>
        /// Helper method to get max Z-Index on canvas
        /// </summary>
        /// <returns>Maximum Z-Index value</returns>
        private int GetMaxZIndex()
        {
            int max = 0;
            foreach (UIElement child in MainInteractiveBoard.Children)
            {
                int zIndex = Panel.GetZIndex(child);
                if (zIndex > max) max = zIndex;
            }
            return max;
        }
        
        /// <summary>
        /// Hide borders of all other image containers
        /// </summary>
        private void HideOtherActionPanels(Grid currentContainer)
        {
            foreach (UIElement child in MainInteractiveBoard.Children)
            {
                if (child is Grid grid && grid != currentContainer)
                {
                    foreach (UIElement gridChild in grid.Children)
                    {
                        // Remove border from imageBorder (Border with Image child)
                        if (gridChild is Border border && border.Child is Image img && img.Tag?.ToString() == "Screenshot")
                        {
                            border.BorderBrush = null;
                            border.BorderThickness = new Thickness(0);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Check if mouse is over an element
        /// </summary>
        private bool IsMouseOverElement(UIElement element, Point mousePosition)
        {
            double left = Canvas.GetLeft(element);
            double top = Canvas.GetTop(element);
            double right = left + ((FrameworkElement)element).ActualWidth;
            double bottom = top + ((FrameworkElement)element).ActualHeight;
            
            return mousePosition.X >= left && mousePosition.X <= right &&
                   mousePosition.Y >= top && mousePosition.Y <= bottom;
        }

        #region Canvas Drag & Drop Integration (UMind Style)

        /// <summary>
        /// QC_4.2_CANVAS_DRAG_DROP: Kích hoạt khả năng Kéo-Thả ảnh (UMind Style) trực tiếp vào bảng viết.
        /// Cho phép giáo viên kéo ảnh từ Google Search Browser, Chrome/Edge bên ngoài, hoặc File Explorer và thả thẳng vào bảng.
        /// </summary>
        public void InitializeCanvasDragAndDrop()
        {
            if (MainInteractiveBoard == null) return;

            MainInteractiveBoard.AllowDrop = true;
            MainInteractiveBoard.DragEnter += MainInteractiveBoard_DragOver;
            MainInteractiveBoard.DragOver += MainInteractiveBoard_DragOver;
            MainInteractiveBoard.Drop += MainInteractiveBoard_Drop;

            System.Diagnostics.Debug.WriteLine("✅ Canvas Drag & Drop initialized (UMind Style)");
        }

        private void MainInteractiveBoard_DragOver(object sender, DragEventArgs e)
        {
            // Kiểm tra xem dữ liệu kéo có chứa hình ảnh hay không
            if (e.Data.GetDataPresent(DataFormats.FileDrop) ||
                e.Data.GetDataPresent(DataFormats.Bitmap) ||
                e.Data.GetDataPresent(DataFormats.Html) ||
                e.Data.GetDataPresent(DataFormats.UnicodeText) ||
                e.Data.GetDataPresent(DataFormats.Text) ||
                e.Data.GetDataPresent("UniformResourceLocator"))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private async void MainInteractiveBoard_Drop(object sender, DragEventArgs e)
        {
            Point dropPoint = e.GetPosition(MainInteractiveBoard);
            e.Handled = true;

            try
            {
                var bitmap = await QASmartTouch.Helpers.SmartImageLoader.ExtractImageFromDataObjectAsync(e.Data);
                if (bitmap != null)
                {
                    InsertInteractiveImage(bitmap, "Kéo thả từ Web / Máy tính", dropPoint.X, dropPoint.Y);
                    ShowSmartStatusBadge("🖼️ Đã chèn hình ảnh kéo thả thành công!");
                    System.Diagnostics.Debug.WriteLine($"✅ Image dropped at ({dropPoint.X:F0}, {dropPoint.Y:F0})");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ No supported image format found in drop data.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error dropping image on canvas: {ex.Message}");
            }
        }

        #endregion
    }
}
