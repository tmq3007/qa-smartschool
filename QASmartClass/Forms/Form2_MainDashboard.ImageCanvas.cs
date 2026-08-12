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

            // === CREATE ACTION PANEL (Above image, outside) ===
            var actionPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, -40, 0, 0), // ✨ Negative margin to place above image
                Background = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)), // Semi-transparent white
                Tag = "ActionPanel", // To identify and exclude from mouse events
                Opacity = 0.35 // ✨ Default faded opacity for touch screens
            };

            // Zoom In Button
            var btnZoomIn = CreateActionButton("➕", "#4CAF50", "Phóng to");
            btnZoomIn.Click += (s, e) =>
            {
                // ✨ FIX: Zoom imageBorder (includes border + image)
                var transform = imageBorder.RenderTransform as ScaleTransform ?? new ScaleTransform(1, 1);
                imageBorder.RenderTransform = transform;
                imageBorder.RenderTransformOrigin = new Point(0.5, 0.5);
                transform.ScaleX *= 1.2;
                transform.ScaleY *= 1.2;
                transform.ScaleX = Math.Min(5.0, transform.ScaleX);
                transform.ScaleY = Math.Min(5.0, transform.ScaleY);
            };

            // Zoom Out Button
            var btnZoomOut = CreateActionButton("➖", "#2196F3", "Thu nhỏ");
            btnZoomOut.Click += (s, e) =>
            {
                // ✨ FIX: Zoom imageBorder (includes border + image)
                var transform = imageBorder.RenderTransform as ScaleTransform ?? new ScaleTransform(1, 1);
                imageBorder.RenderTransform = transform;
                imageBorder.RenderTransformOrigin = new Point(0.5, 0.5);
                transform.ScaleX *= 0.8;
                transform.ScaleY *= 0.8;
                transform.ScaleX = Math.Max(0.1, transform.ScaleX);
                transform.ScaleY = Math.Max(0.1, transform.ScaleY);
            };

            actionPanel.Children.Add(btnZoomIn);
            actionPanel.Children.Add(btnZoomOut);

            // Add action panel to container
            container.Children.Add(actionPanel);

            // Set initial position (center of canvas)
            double x = (MainInteractiveBoard.ActualWidth - container.Width) / 2;
            double y = (MainInteractiveBoard.ActualHeight - container.Height) / 2;
            Canvas.SetLeft(container, Math.Max(0, x));
            Canvas.SetTop(container, Math.Max(0, y));

            // === SHOW/HIDE ACTION PANEL + DRAG TO MOVE ===
            Point? dragStart = null;
            Point? originalPosition = null;

            container.MouseLeftButtonDown += (s, e) =>
            {
                // Do not drag image when in Pen, Eraser, or Shape Drawing mode
                if (_drawingEnabled || _eraserEnabled || _shapeDrawingEnabled) return;

                // Only process if not clicking on a button or its children
                if (QASmartTouch.Utilities.InputValidationHelper.IsEventFromInteractiveControl(e.OriginalSource, container)) return;
                
                // Show this image's action panel and border
                actionPanel.Opacity = 1.0;
                imageBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // #2196F3
                imageBorder.BorderThickness = new Thickness(2);
                
                // Hide all other images' action panels and borders
                HideOtherActionPanels(container);
                
                // Start dragging
                dragStart = e.GetPosition(MainInteractiveBoard);
                originalPosition = new Point(Canvas.GetLeft(container), Canvas.GetTop(container));
                container.CaptureMouse();
                Panel.SetZIndex(container, GetMaxZIndex() + 1);
                
                e.Handled = true;
            };

            // Hover effects to highlight action panel when mouse is over image container
            container.MouseEnter += (s, e) =>
            {
                actionPanel.Opacity = 1.0;
            };
            container.MouseLeave += (s, e) =>
            {
                // Only fade if the image is not currently selected (has no blue border)
                if (imageBorder.BorderThickness.Left == 0)
                {
                    actionPanel.Opacity = 0.35;
                }
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
            
            // === HIDE ACTION PANEL WHEN CLICKING OUTSIDE (Memory leak free) ===
            MouseButtonEventHandler clickOutsideHandler = null!;
            clickOutsideHandler = (s, e) =>
            {
                // If click is not on this container, hide its action panel
                if (!IsMouseOverElement(container, e.GetPosition(MainInteractiveBoard)))
                {
                    actionPanel.Opacity = 0.35;
                    imageBorder.BorderBrush = null;
                    imageBorder.BorderThickness = new Thickness(0);
                }
            };
            MainInteractiveBoard.MouseLeftButtonDown += clickOutsideHandler;

            // Delete Button - registered after clickOutsideHandler to safely unsubscribe it
            var btnDelete = CreateActionButton("🗑️", "#EE5A6F", "Xóa ảnh");
            btnDelete.Click += (s, e) =>
            {
                if (container.IsMouseCaptured)
                {
                    container.ReleaseMouseCapture();
                }
                MainInteractiveBoard.Children.Remove(container);
                MainInteractiveBoard.MouseLeftButtonDown -= clickOutsideHandler; // Clean up delegate to prevent memory leak
                try { RecordRemoveAction(container, "Xóa ảnh chụp màn hình"); } catch { }
                e.Handled = true;
            };
            actionPanel.Children.Add(btnDelete);

            // === ADD TO CANVAS ===
            MainInteractiveBoard.Children.Add(container);
            try { RegisterNewObjectWithSelectionManager(container); } catch { }
            try { RecordAddAction(container, "Thêm ảnh chụp màn hình"); } catch { }
        }

        /// <summary>
        /// Helper method to create action button
        /// </summary>
        private Button CreateActionButton(string content, string bgColor, string tooltip)
        {
            var button = new Button
            {
                Content = content,
                Width = 32,
                Height = 32,
                Margin = new Thickness(2),
                FontSize = 14,
                Cursor = Cursors.Hand,
                ToolTip = tooltip,
                BorderThickness = new Thickness(0)
            };

            // Parse color
            var color = (Color)ColorConverter.ConvertFromString(bgColor);
            button.Background = new SolidColorBrush(color);

            // Template for rounded button
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));

            var content_presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            content_presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content_presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            border.AppendChild(content_presenter);
            template.VisualTree = border;
            button.Template = template;

            return button;
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
        /// Hide action panels of all other image containers
        /// </summary>
        private void HideOtherActionPanels(Grid currentContainer)
        {
            foreach (UIElement child in MainInteractiveBoard.Children)
            {
                if (child is Grid grid && grid != currentContainer)
                {
                    // Find action panel and image border in this grid
                    foreach (UIElement gridChild in grid.Children)
                    {
                        // Fade action panel
                        if (gridChild is StackPanel panel && panel.Tag?.ToString() == "ActionPanel")
                        {
                            panel.Opacity = 0.35;
                        }
                        
                        // ✨ FIX: Remove border from imageBorder (Border with Image child)
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
    }
}
