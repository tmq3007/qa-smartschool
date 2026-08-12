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
        /// Insert a 3D model onto the canvas
        /// </summary>
        public void Insert3DModelToCanvas(string modelPath, Model3DGroup model3D)
        {
            try
            {
                // Create 3D model control
                var modelControl = new Controls.Model3DControl(modelPath, model3D);
                
                // Set initial position (center of canvas)
                double x = (MainInteractiveBoard.ActualWidth - modelControl.Width) / 2;
                double y = (MainInteractiveBoard.ActualHeight - modelControl.Height) / 2;
                
                Canvas.SetLeft(modelControl, x);
                Canvas.SetTop(modelControl, y);
                Canvas.SetZIndex(modelControl, MainInteractiveBoard.Children.Count);
                
                // Add to canvas
                MainInteractiveBoard.Children.Add(modelControl);
                
                // Enable dragging - DISABLED to allow viewport interaction
                // modelControl.MouseLeftButtonDown += Model3DControl_MouseLeftButtonDown;
                // modelControl.MouseMove += Model3DControl_MouseMove;
                // modelControl.MouseLeftButtonUp += Model3DControl_MouseLeftButtonUp;
                
                // Handle delete request
                modelControl.DeleteRequested += (s, e) =>
                {
                    MainInteractiveBoard.Children.Remove(modelControl);
                    System.Diagnostics.Debug.WriteLine($"[MainDashboard] 3D Model removed: {modelControl.ModelData.FileName}");
                };
                
                System.Diagnostics.Debug.WriteLine($"✅ [MainDashboard] 3D Model inserted: {modelControl.ModelData.FileName} at ({x}, {y})");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi chèn mô hình 3D:\n{ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ [MainDashboard] Error inserting 3D model: {ex.Message}");
            }
        }

        // Drag support for 3D models
        private Controls.Model3DControl? _draggingModel3D;
        private Point _dragModel3DStartPoint;

        private void Model3DControl_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Controls.Model3DControl modelControl)
            {
                _draggingModel3D = modelControl;
                _dragModel3DStartPoint = e.GetPosition(MainInteractiveBoard);
                modelControl.CaptureMouse();
                e.Handled = true;
                Canvas.SetZIndex(modelControl, MainInteractiveBoard.Children.Count);
                System.Diagnostics.Debug.WriteLine($"[MainDashboard] Started dragging 3D model");
            }
        }

        private void Model3DControl_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_draggingModel3D != null && e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(MainInteractiveBoard);
                double offsetX = currentPoint.X - _dragModel3DStartPoint.X;
                double offsetY = currentPoint.Y - _dragModel3DStartPoint.Y;
                
                double newLeft = Canvas.GetLeft(_draggingModel3D) + offsetX;
                double newTop = Canvas.GetTop(_draggingModel3D) + offsetY;
                
                // Constrain to canvas bounds
                newLeft = Math.Max(0, Math.Min(newLeft, MainInteractiveBoard.ActualWidth - _draggingModel3D.Width));
                newTop = Math.Max(0, Math.Min(newTop, MainInteractiveBoard.ActualHeight - _draggingModel3D.Height));
                
                Canvas.SetLeft(_draggingModel3D, newLeft);
                Canvas.SetTop(_draggingModel3D, newTop);
                
                _dragModel3DStartPoint = currentPoint;
                e.Handled = true;
            }
        }

        private void Model3DControl_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_draggingModel3D != null)
            {
                _draggingModel3D.ReleaseMouseCapture();
                Point finalPosition = new Point(Canvas.GetLeft(_draggingModel3D), Canvas.GetTop(_draggingModel3D));
                _draggingModel3D.UpdateModelData(finalPosition);
                System.Diagnostics.Debug.WriteLine($"[MainDashboard] Finished dragging 3D model to ({finalPosition.X}, {finalPosition.Y})");
                _draggingModel3D = null;
                e.Handled = true;
            }
        }

        #endregion
    }
}
