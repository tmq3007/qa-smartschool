using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace QASmartTouch.Forms
{
    /// <summary>
    /// Extension methods for Form2_MainDashboard - 3D Model Support
    /// </summary>
    public partial class Form2_MainDashboard
    {
        #region 3D Model Management

        /// <summary>
        /// Insert a 3D model onto the canvas using UMind Unified Container standard
        /// </summary>
        public void Insert3DModelToCanvas(string modelPath, Model3DGroup model3D)
        {
            try
            {
                var modelControl = new Controls.Model3DControl(modelPath, model3D);

                // Position in the center of the currently visible viewport
                double viewWidth = MainScrollViewer?.ViewportWidth > 0 ? MainScrollViewer.ViewportWidth : MainInteractiveBoard.ActualWidth;
                double viewHeight = MainScrollViewer?.ViewportHeight > 0 ? MainScrollViewer.ViewportHeight : MainInteractiveBoard.ActualHeight;
                double offsetX = MainScrollViewer?.HorizontalOffset ?? 0;
                double offsetY = MainScrollViewer?.VerticalOffset ?? 0;

                double x = Math.Max(40, offsetX + (viewWidth - modelControl.Width) / 2);
                double y = Math.Max(40, offsetY + (viewHeight - modelControl.Height) / 2);

                Canvas.SetLeft(modelControl, x);
                Canvas.SetTop(modelControl, y);
                Panel.SetZIndex(modelControl, AllocateMediaWidgetZIndex());
                modelControl.UpdateModelData(new Point(x, y));

                MainInteractiveBoard.Children.Add(modelControl);

                RecordAddAction(modelControl, $"3D Model: {modelControl.ModelData.FileName}");
                RefreshSelectableObjects();

                // Bring to front on header drag or viewport interaction
                modelControl.BringToFrontRequested += (s, e) =>
                {
                    Panel.SetZIndex(modelControl, AllocateMediaWidgetZIndex());
                };

                // Sync SelectionManager & QuadTree after header drag or resize
                modelControl.PositionOrSizeChanged += (s, e) =>
                {
                    RefreshSelectableObjects();
                };

                // Handle 1-tap close/delete request with Undo/Redo support
                modelControl.DeleteRequested += (s, e) =>
                {
                    modelControl.StopAutoRotate();
                    if (MainInteractiveBoard.Children.Contains(modelControl))
                    {
                        MainInteractiveBoard.Children.Remove(modelControl);
                        RecordRemoveAction(modelControl, $"Deleted 3D Model: {modelControl.ModelData.FileName}");
                        RefreshSelectableObjects();
                    }
                    System.Diagnostics.Debug.WriteLine($"[MainDashboard] 3D Model removed: {modelControl.ModelData.FileName}");
                };

                System.Diagnostics.Debug.WriteLine($"✅ [MainDashboard] 3D Model inserted: {modelControl.ModelData.FileName} at ({x:F0}, {y:F0})");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi chèn mô hình 3D:\n{ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ [MainDashboard] Error inserting 3D model: {ex.Message}");
            }
        }

        #endregion
    }
}
