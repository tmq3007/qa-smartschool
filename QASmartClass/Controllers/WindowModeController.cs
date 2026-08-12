using System;
using System.Windows;
using System.Drawing;
using QASmartTouch.Services;

namespace QASmartTouch.Controllers
{
    /// <summary>
    /// Controls Window Mode functionality
    /// Manages floating toolbar and main app state
    /// </summary>
    public class WindowModeController
    {
        private Forms.FloatingToolbarWindow? _toolbar;
        private Forms.AnnotationOverlay? _overlay;
        private Forms.ColorPickerPopup? _colorPicker;
        private Forms.Form2_MainDashboard _mainApp;
        private Services.ScreenCaptureService? _screenCapture;
        private Services.ScreenRecorderService? _screenRecorder;
        private System.Windows.Threading.DispatcherTimer? _recordingTimer;
        private bool _isWindowModeActive = false;
        private bool _isPenMode = false;
        private bool _isMouseMode = false;
        private bool _isEraseByClickActive = false;  // Chế độ Xóa Từng Nét

        public bool IsWindowModeActive => _isWindowModeActive;
        public bool IsPenMode => _isPenMode;
        public bool IsMouseMode => _isMouseMode;

        public event EventHandler? WindowModeExited;

        public WindowModeController(Forms.Form2_MainDashboard mainApp)
        {
            _mainApp = mainApp ?? throw new ArgumentNullException(nameof(mainApp));
            
            // Initialize services
            _screenCapture = new Services.ScreenCaptureService();
            _screenRecorder = new Services.ScreenRecorderService();
            
            System.Diagnostics.Debug.WriteLine("✅ WindowModeController initialized with services");
        }

        /// <summary>
        /// Enter Window Mode
        /// </summary>
        public void EnterWindowMode()
        {
            if (_isWindowModeActive)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Already in Window Mode");
                return;
            }

            try
            {
                // Minimize main app
                _mainApp.WindowState = WindowState.Minimized;
                
                // Create and show annotation overlay (with semi-transparent background)
                _overlay = new Forms.AnnotationOverlay();
                _overlay.Show();
                
                // Subscribe to overlay events
                _overlay.ToolbarRepositionRequested += OnToolbarRepositionRequested;
                if (_overlay.UndoRedoManager != null)
                {
                    _overlay.UndoRedoManager.UndoStackChanged += OnOverlayUndoRedoStackChanged;
                    _overlay.UndoRedoManager.RedoStackChanged += OnOverlayUndoRedoStackChanged;
                }
                
                // Create and show toolbar
                _toolbar = new Forms.FloatingToolbarWindow();
                
                // Subscribe to events
                _toolbar.BackToMainApp += OnBackToMainApp;
                _toolbar.PenSelected += OnPenSelected;
                _toolbar.ShapesSelected += OnShapesSelected;
                _toolbar.FillSelected += OnFillSelected;
                _toolbar.ClearAllRequested += OnClearAllRequested;
                _toolbar.UndoRequested += OnUndoRequested;
                _toolbar.RedoRequested += OnRedoRequested;
                _toolbar.ScreenshotRequested += OnScreenshotRequested;
                _toolbar.WindowModeToggled += OnWindowModeToggled;
                _toolbar.SelectDisplayRequested += OnSelectDisplayRequested;
                _toolbar.SelectAreaRequested += OnSelectAreaRequested;
                _toolbar.RecordToggled += OnRecordToggled;
                _toolbar.DeleteLastStrokeRequested += OnDeleteLastStrokeRequested;
                _toolbar.MouseModeRequested += OnMouseModeRequested;
                
                // Subscribe Save event
                if (_screenRecorder != null)
                    _screenRecorder.SaveCompleted += OnRecordingSaveCompleted;
                
                if (_overlay?.UndoRedoManager != null)
                {
                    _toolbar.UpdateUndoRedoButtonsState(_overlay.UndoRedoManager.CanUndo, _overlay.UndoRedoManager.CanRedo);
                }
                else
                {
                    _toolbar.UpdateUndoRedoButtonsState(false, false);
                }

                _toolbar.Show();
                
                _isWindowModeActive = true;
                
                System.Diagnostics.Debug.WriteLine("✅ Entered Window Mode - Ready for screenshot");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error entering Window Mode: {ex.Message}");
                MessageBox.Show($"Lỗi khi bật Window Mode: {ex.Message}", 
                              "Lỗi", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Exit Window Mode
        /// </summary>
        public void ExitWindowMode()
        {
            if (!_isWindowModeActive)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Not in Window Mode");
                return;
            }

            try
            {
                // 1. Dừng ghi hình an toàn nếu đang chạy
                if (_screenRecorder != null && _screenRecorder.IsRecording)
                {
                    _recordingTimer?.Stop();
                    _recordingTimer = null;
                    _screenRecorder.StopRecording();
                    System.Diagnostics.Debug.WriteLine("⏹️ Screen recording stopped safely upon toolbar close");
                }

                // 2. Hỏi lưu nháp nếu phát hiện có nét vẽ trên bảng
                if (_overlay != null && _overlay.UndoRedoManager != null && _overlay.UndoRedoManager.CanUndo)
                {
                    var confirm = MessageBox.Show(
                        "Bạn có các nét vẽ chưa lưu trên màn hình. Bạn có muốn lưu lại ảnh chụp màn hình kèm chú thích trước khi thoát không?",
                        "Xác nhận thoát",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);
                        
                    if (confirm == MessageBoxResult.Cancel)
                    {
                        return; // Hủy lệnh đóng
                    }
                    else if (confirm == MessageBoxResult.Yes)
                    {
                        OnScreenshotRequested(this, EventArgs.Empty); // Gọi chụp lưu màn hình
                    }
                }

                // Close overlay
                if (_overlay != null)
                {
                    // Unsubscribe from overlay events
                    if (_overlay.UndoRedoManager != null)
                    {
                        _overlay.UndoRedoManager.UndoStackChanged -= OnOverlayUndoRedoStackChanged;
                        _overlay.UndoRedoManager.RedoStackChanged -= OnOverlayUndoRedoStackChanged;
                    }
                    _overlay.ToolbarRepositionRequested -= OnToolbarRepositionRequested;
                    
                    _overlay.Close();
                    _overlay = null;
                }
                
                // Close toolbar
                if (_toolbar != null)
                {
                    // Unsubscribe from events
                    _toolbar.BackToMainApp -= OnBackToMainApp;
                    _toolbar.PenSelected -= OnPenSelected;
                    _toolbar.ShapesSelected -= OnShapesSelected;
                    _toolbar.FillSelected -= OnFillSelected;
                    _toolbar.ClearAllRequested -= OnClearAllRequested;
                    _toolbar.UndoRequested -= OnUndoRequested;
                    _toolbar.RedoRequested -= OnRedoRequested;
                    _toolbar.ScreenshotRequested -= OnScreenshotRequested;
                    _toolbar.WindowModeToggled -= OnWindowModeToggled;
                    _toolbar.SelectDisplayRequested -= OnSelectDisplayRequested;
                    _toolbar.SelectAreaRequested -= OnSelectAreaRequested;
                    _toolbar.RecordToggled -= OnRecordToggled;
                    _toolbar.DeleteLastStrokeRequested -= OnDeleteLastStrokeRequested;
                    _toolbar.MouseModeRequested -= OnMouseModeRequested;
                    
                    _toolbar.Close();
                    _toolbar = null;
                }
                
                // Close color picker
                if (_colorPicker != null)
                {
                    _colorPicker.Close();
                    _colorPicker = null;
                }
                
                // Restore main app
                _mainApp.WindowState = WindowState.Maximized;
                _mainApp.Activate();
                
                _isWindowModeActive = false;
                _isPenMode = false;
                _isMouseMode = false;
                _isEraseByClickActive = false;
                
                WindowModeExited?.Invoke(this, EventArgs.Empty);
                
                System.Diagnostics.Debug.WriteLine("✅ Exited Window Mode");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error exiting Window Mode: {ex.Message}");
            }
        }

        #region Event Handlers

        private void OnBackToMainApp(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🔙 Back to main app requested");
            ExitWindowMode();
        }

        /// <summary>
        /// H\u00e0m trung t\u00e2m: T\u1eaft T\u1ea4T C\u1ea2 c\u00e1c ch\u1ebf \u0111\u1ed9 hi\u1ec7n t\u1ea1i v\u00e0 reset UI
        /// Sau khi g\u1ecdi: overlay \u1edf tr\u1ea1ng th\u00e1i click-through (hi\u1ec7n nh\u01b0ng kh\u00f4ng ch\u1eb7n chu\u1ed9t)
        /// </summary>
        private void DeactivateAllModes()
        {
            // 1. T\u1eaft Erase-by-Click n\u1ebfu \u0111ang b\u1eadt
            if (_isEraseByClickActive)
            {
                _isEraseByClickActive = false;
                _overlay?.ToggleEraseByClickMode(); // g\u1ecdi l\u1ea7n 2 \u0111\u1ec3 t\u1eaft trong overlay
                System.Diagnostics.Debug.WriteLine("\u274c Erase mode deactivated");
            }

            // 2. Reset t\u1ea5t c\u1ea3 flag
            _isPenMode   = false;
            _isMouseMode = false;

            // 3. \u0110\u01b0a overlay v\u1ec1 tr\u1ea1ng th\u00e1i s\u1ea1ch:
            //    - Click-through b\u1eadt (kh\u00f4ng ch\u1eb7n chu\u1ed9t)
            //    - Cursor v\u1ec1 Arrow
            //    - Overlay v\u1eabn hi\u1ec7n \u0111\u1ec3 th\u1ea5y annotation
            _overlay?.SetPenMode(false);   // t\u1eaft pen \u2192 b\u1eadt click-through, cursor arrow
            // Kh\u00f4ng c\u1ea7n g\u1ecdi SetMouseMode v\u00ec SetPenMode(false) \u0111\u00e3 b\u1eadt click-through r\u1ed3i

            // 4. Reset n\u00fat toolbar
            _toolbar?.SetActiveToolButton("none");

            System.Diagnostics.Debug.WriteLine("\u2705 All modes deactivated \u2013 overlay in click-through state");
        }

        private void OnMouseModeToggled(object? sender, EventArgs e)
        {
            // Legacy handler - delegate to new one
            OnMouseModeRequested(sender, e);
        }

        private void OnMouseModeRequested(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🖥️ Mouse mode requested");

            if (_overlay == null) return;

            if (_isMouseMode)
            {
                // Đang ở mouse mode → tắt đi (toggle off)
                DeactivateAllModes();
                System.Diagnostics.Debug.WriteLine("🖥️ Mouse mode OFF");
            }
            else
            {
                // Tắt tất cả các mode khác
                DeactivateAllModes();

                // Bật Mouse mode: overlay không chặn click, cho phép tương tác với cửa sổ bên dưới
                _isMouseMode = true;
                _overlay.SetMouseMode(true);

                _toolbar?.SetActiveToolButton("mouse");
                _overlay?.ShowToast("🖥️ Chế độ chuột – bạn có thể di chuyển và tương tác màn hình", "#388E3C");
                System.Diagnostics.Debug.WriteLine("🖥️ Mouse mode ON");
            }
        }
        
        private void OnToolbarRepositionRequested(object? sender, Forms.ToolbarSideEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"🔄 Toolbar reposition requested: {(e.IsLeftSide ? "LEFT" : "RIGHT")} side");
            _toolbar?.MoveToolbarToSide(e.IsLeftSide);
        }

        private void OnPenSelected(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("✏️ Pen selected");

            if (_overlay == null) return;

            // Tắt tất cả chế độ hiện tại
            DeactivateAllModes();

            // Bật Pen mode
            _isPenMode = true;
            _overlay.SetPenMode(true);
            _overlay.SetTool(Forms.AnnotationTool.Pen);

            // Highlight nút Pen
            _toolbar?.SetActiveToolButton("pen");

            // Hiển color picker
            ShowColorPicker();
        }

        private void OnShapesSelected(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("📐 Shapes selected");
            MessageBox.Show("Công cụ hình vẽ sẽ được triển khai trong Phase 5", 
                          "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnFillSelected(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🎨 Fill selected");
            MessageBox.Show("Công cụ tô màu sẽ được triển khai trong Phase 5", 
                          "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnClearAllRequested(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🗑️ Clear all requested");

            var result = MessageBox.Show(
                "Bạn có chắc muốn xóa tất cả vẽ?\n\nHành động này không thể hoàn tác.",
                "Xóa tất cả", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _overlay?.ClearAll();
                System.Diagnostics.Debug.WriteLine("✅ All annotations cleared");
            }
        }

        private void OnUndoRequested(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("↶ Undo requested");
            _overlay?.Undo();
        }

        private void OnDeleteLastStrokeRequested(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🧹 Toggle Erase-by-Click mode");

            if (!_isEraseByClickActive)
            {
                // Vào chế độ xóa:
                // 1. Tắt các chế độ khác (nhưng giữ overlay visible)
                if (_isEraseByClickActive == false && _isPenMode)
                {
                    // Đang ở pen mode, chỉ cần tắt draw, không tắt overlay
                    _isPenMode = false;
                    _overlay?.SetPenMode(false);
                }
                if (_isMouseMode)
                {
                    _isMouseMode = false;
                    _overlay?.SetMouseMode(false);
                }

                // 2. Đảm bảo overlay nhận được click
                _overlay?.SetClickCaptureOnly(true);

                // 3. Bật chế độ xóa
                _isEraseByClickActive = true;
                _overlay?.ToggleEraseByClickMode();

                // 4. Highlight nút Erase
                _toolbar?.SetActiveToolButton("erase");
            }
            else
            {
                // Thoát chế độ xóa:
                _isEraseByClickActive = false;
                _overlay?.ToggleEraseByClickMode();
                _overlay?.SetClickCaptureOnly(false);

                // Reset tất cả nút
                _toolbar?.SetActiveToolButton("none");
            }
        }

        private void OnRedoRequested(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("↷ Redo requested");
            _overlay?.Redo();
        }

        private void OnScreenshotRequested(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("📷 Screenshot requested");

            try
            {
                // 1. Ẩn overlay và toolbar để chụp màn hình sạch
                _overlay?.Hide();
                _toolbar?.Hide();

                // 2. Chờ render xử lý xong
                System.Threading.Thread.Sleep(200);
                Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);

                // 3. Chụp màn hình thực (physical pixels)
                System.Windows.Media.Imaging.BitmapSource? screenshot = null;
                using (var g0 = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
                {
                    double dpiX = g0.DpiX / 96.0;
                    double dpiY = g0.DpiY / 96.0;
                    int physW = (int)(SystemParameters.PrimaryScreenWidth  * dpiX);
                    int physH = (int)(SystemParameters.PrimaryScreenHeight * dpiY);

                    System.Diagnostics.Debug.WriteLine($"📸 Capturing screen {physW}x{physH} (DPI {dpiX*100:F0}%)");

                    using (var bmp = new System.Drawing.Bitmap(physW, physH,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                    {
                        using (var g = System.Drawing.Graphics.FromImage(bmp))
                            g.CopyFromScreen(0, 0, 0, 0, bmp.Size, System.Drawing.CopyPixelOperation.SourceCopy);

                        var hBmp = bmp.GetHbitmap();
                        try
                        {
                            screenshot = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                hBmp, IntPtr.Zero, Int32Rect.Empty,
                                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                            screenshot.Freeze();
                        }
                        finally { DeleteObject(hBmp); }
                    }
                }

                // 4. Hiển lại trước khi show dialog
                _overlay?.Show();
                _toolbar?.Show();

                if (screenshot == null)
                {
                    MessageBox.Show("Không thể chụp ảnh", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 5. Dialog lưu file
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PNG Image|*.png|JPEG Image|*.jpg|All Files|*.*",
                    DefaultExt = ".png",
                    FileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    using (var fs = new System.IO.FileStream(dialog.FileName, System.IO.FileMode.Create))
                    {
                        System.Windows.Media.Imaging.BitmapEncoder encoder =
                            dialog.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                            dialog.FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                                ? new System.Windows.Media.Imaging.JpegBitmapEncoder()
                                : (System.Windows.Media.Imaging.BitmapEncoder)new System.Windows.Media.Imaging.PngBitmapEncoder();

                        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(screenshot));
                        encoder.Save(fs);
                    }

                    System.Diagnostics.Debug.WriteLine($"✅ Screenshot saved: {dialog.FileName}");

                    var res = MessageBox.Show(
                        $"Đã lưu ảnh tại:\n{dialog.FileName}\n\nBạn có muốn mở thư mục?",
                        "Chụp ảnh thành công", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (res == MessageBoxResult.Yes)
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{dialog.FileName}\"");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Screenshot error: {ex.Message}\n{ex.StackTrace}");
                _overlay?.Show();
                _toolbar?.Show();
                MessageBox.Show($"Lỗi khi chụp ảnh:\n{ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnWindowModeToggled(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🪟 Window Mode toggle requested");
            
            // Since this button only appears in Window Mode toolbar,
            // clicking it means user wants to exit Window Mode
            ExitWindowMode();
        }

        private void OnSelectDisplayRequested(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("📺 Select display requested");
            // TODO: Implement in Phase 4
            MessageBox.Show("Chọn màn hình sẽ được triển khai trong Phase 4", 
                          "Thông báo", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Information);
        }

        private async void OnSelectAreaRequested(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("📋 Select area requested");
            
            try
            {
                if (_overlay == null)
                {
                    MessageBox.Show("Overlay chưa sẵn sàng", 
                                  "Lỗi", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Error);
                    return;
                }
                
                // Hide overlay temporarily
                System.Diagnostics.Debug.WriteLine("🙈 Hiding overlay and toolbar...");
                _overlay.Visibility = Visibility.Hidden;
                _toolbar?.Hide();
                
                // Wait for UI to update
                await System.Threading.Tasks.Task.Delay(100);
                
                // Show area selection window
                System.Diagnostics.Debug.WriteLine("📐 Showing area selection window...");
                var selectionWindow = new Forms.AreaSelectionWindow();
                var result = selectionWindow.ShowDialog();
                
                // Add a tiny delay to allow AreaSelectionWindow to fully close and disappear
                await System.Threading.Tasks.Task.Delay(50);
                
                if (result == true && !selectionWindow.WasCancelled)
                {
                    var selectedArea = selectionWindow.SelectedArea;
                    System.Diagnostics.Debug.WriteLine($"📏 Selected area: X={selectedArea.X}, Y={selectedArea.Y}, W={selectedArea.Width}, H={selectedArea.Height}");
                    
                    // Capture the selected area from screen (while overlay and toolbar are still hidden)
                    System.Diagnostics.Debug.WriteLine("📸 Capturing screen area...");
                    var capturedImage = CaptureScreenArea(selectedArea);
                    
                    if (capturedImage != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Image captured: {capturedImage.PixelWidth}x{capturedImage.PixelHeight}");
                        
                        // Exit Window Mode
                        System.Diagnostics.Debug.WriteLine("🚪 Exiting Window Mode...");
                        ExitWindowMode();
                        
                        // Pass image to MainDashboard
                        System.Diagnostics.Debug.WriteLine("📸 Passing image to MainDashboard...");
                        if (_mainApp != null)
                        {
                            // Call method on MainDashboard to add image
                            _mainApp.AddCapturedImage(capturedImage);
                            System.Diagnostics.Debug.WriteLine("✅ Image passed to MainDashboard");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("⚠️ MainApp is null, cannot pass image");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("❌ Captured image is NULL!");
                        MessageBox.Show("Không thể chụp vùng đã chọn", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        
                        // Restore overlay and toolbar since capture failed
                        System.Diagnostics.Debug.WriteLine("👁️ Restoring overlay and toolbar after capture failure...");
                        _overlay.Visibility = Visibility.Visible;
                        _toolbar?.Show();
                    }
                }
                else
                {
                    // Restore overlay and toolbar since selection was cancelled
                    System.Diagnostics.Debug.WriteLine("👁️ Restoring overlay and toolbar after cancel...");
                    _overlay.Visibility = Visibility.Visible;
                    _toolbar?.Show();
                    System.Diagnostics.Debug.WriteLine("ℹ️ Area selection cancelled");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Area selection error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Lỗi khi chọn vùng:\n{ex.Message}",
                              "Lỗi",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                              
                // Ensure overlay is visible
                if (_overlay != null)
                {
                    _overlay.Visibility = Visibility.Visible;
                }
                _toolbar?.Show();
            }
        }
        
        private System.Windows.Media.Imaging.BitmapSource? CaptureScreenArea(Rect area)
        {
            try
            {
                // SelectedArea \u0111\u00e3 \u1edf d\u1ea1ng physical pixels (t\u1eeb PointToScreen)
                // N\u00ean d\u00f9ng tr\u1ef1c ti\u1ebfp kh\u00f4ng c\u1ea7n nh\u00e2n DPI
                int physX      = (int)Math.Round(area.X);
                int physY      = (int)Math.Round(area.Y);
                int physWidth  = (int)Math.Round(area.Width);
                int physHeight = (int)Math.Round(area.Height);

                System.Diagnostics.Debug.WriteLine(
                    $"\ud83c\udfaf CaptureScreenArea (physical px): X={physX}, Y={physY}, W={physWidth}, H={physHeight}");

                if (physWidth <= 0 || physHeight <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"\u274c Invalid area size: {physWidth}x{physHeight}");
                    return null;
                }

                using (var bmp = new System.Drawing.Bitmap(
                    physWidth, physHeight,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    using (var gr = System.Drawing.Graphics.FromImage(bmp))
                    {
                        gr.CopyFromScreen(
                            physX, physY,
                            0, 0,
                            bmp.Size,
                            System.Drawing.CopyPixelOperation.SourceCopy);
                    }

                    var hBitmap = bmp.GetHbitmap();
                    try
                    {
                        var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                            hBitmap,
                            IntPtr.Zero,
                            Int32Rect.Empty,
                            System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                        System.Diagnostics.Debug.WriteLine($"\u2705 Captured: {bitmapSource.PixelWidth}x{bitmapSource.PixelHeight}");
                        return bitmapSource;
                    }
                    finally
                    {
                        DeleteObject(hBitmap);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"\u274c Capture error: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }
        
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private void OnRecordToggled(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("⏺️ Record toggled");
            
            if (_screenRecorder == null)
            {
                MessageBox.Show("Dịch vụ ghi màn hình chưa sẵn sàng", 
                              "Lỗi", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
                return;
            }
            
            if (_screenRecorder.IsRecording)
            {
                StopRecording();
            }
            else
            {
                // Ask user for format
                var result = MessageBox.Show(
                    "Chọn định dạng ghi màn hình:\n\n" +
                    "📹 GIF (Optimized)\n" +
                    "   • Tương thích cao\n" +
                    "   • Đã tối ưu (ít chấm nhiễu)\n" +
                    "   • File size: Trung bình\n\n" +
                    "🎯 WebP (Zero Dithering)\n" +
                    "   • Không có chấm nhiễu\n" +
                    "   • Chất lượng hoàn hảo\n" +
                    "   • File size: Nhỏ hơn\n\n" +
                    "Chọn YES cho WebP, NO cho GIF",
                    "Chọn định dạng",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Cancel)
                    return;
                
                // Set format
                var format = result == MessageBoxResult.Yes ? RecordingFormat.WebP : RecordingFormat.GIF;
                _screenRecorder.SetRecordingFormat(format);
                
                StartRecording();
            }
        }

        #endregion

        #region Recording Methods

        private void StartRecording()
        {
            try
            {
                _screenRecorder?.StartRecording();
                _toolbar?.SetRecordingState(true);

                // Timer hiển thị thời gian ghi
                _recordingTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                _recordingTimer.Tick += RecordingTimer_Tick;
                _recordingTimer.Start();

                var format = _screenRecorder?.CurrentFormat ?? RecordingFormat.GIF;
                var formatName = format == RecordingFormat.WebP ? "WebP" : "GIF";

                // Hiển toast nhỏ thay vì MessageBox toàn màn hình
                _overlay?.ShowToast($"⏺️ Đang ghi màn hình ({formatName}) | Nhấn nút ● lại để dừng", "#5C6BC0");

                System.Diagnostics.Debug.WriteLine($"⏺️ Recording started ({formatName})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Start recording error: {ex.Message}");
                _toolbar?.SetRecordingState(false);
                MessageBox.Show($"Lỗi khi bắt đầu ghi:\n{ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopRecording()
        {
            try
            {
                _recordingTimer?.Stop();
                _recordingTimer = null;

                // StopRecording() trả về ngay, lưu file trong background
                _screenRecorder?.StopRecording();
                _toolbar?.SetRecordingState(false);

                // Thông báo cho user biết đang xử lý
                _overlay?.ShowToast("💾 Đang lưu file ghi màn hình...", "#E65100");

                System.Diagnostics.Debug.WriteLine("⏹️ Recording stopped, saving in background...");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Stop recording error: {ex.Message}");
                MessageBox.Show($"Lỗi khi dừng ghi:\n{ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnRecordingSaveCompleted(string? savedPath)
        {
            System.Diagnostics.Debug.WriteLine($"✅ Recording save completed: {savedPath}");

            if (savedPath != null && System.IO.File.Exists(savedPath))
            {
                var res = MessageBox.Show(
                    $"Đã lưu file tại:\n{savedPath}\n\nBạn có muốn mở thư mục?",
                    "Ghi màn hình hoàn tất", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (res == MessageBoxResult.Yes)
                    _screenRecorder?.OpenRecordingsFolder();
            }
            else
            {
                MessageBox.Show(
                    savedPath == null
                        ? "Không có frames nào được ghi. Vui lòng thử lại."
                        : $"Lỗi: File không được tạo!\n{savedPath}",
                    "Lỗi ghi màn hình", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RecordingTimer_Tick(object? sender, EventArgs e)
        {
            // Update UI with recording time
            var time = _screenRecorder?.GetFormattedRecordingTime() ?? "00:00";
            System.Diagnostics.Debug.WriteLine($"⏱️ Recording: {time}");
            
            // TODO: Update toolbar with time display (Phase 5)
        }

        #endregion

        #region Color Picker

        private void ShowColorPicker()
        {
            try
            {
                // Close existing picker if any
                if (_colorPicker != null)
                {
                    _colorPicker.Close();
                    _colorPicker = null;
                }

                // Create new color picker
                _colorPicker = new Forms.ColorPickerPopup();
                
                // Subscribe to events
                _colorPicker.ColorChanged += OnColorChanged;
                _colorPicker.ThicknessChanged += OnThicknessChanged;
                _colorPicker.Closed += OnColorPickerClosed;
                
                // Position near toolbar
                if (_toolbar != null)
                {
                    _colorPicker.Left = _toolbar.Left - _colorPicker.Width - 20;
                    _colorPicker.Top = _toolbar.Top + 100;
                }
                
                // Show popup
                _colorPicker.Show();
                
                System.Diagnostics.Debug.WriteLine("🎨 Color picker shown");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error showing color picker: {ex.Message}");
            }
        }

        private void OnColorPickerClosed(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🎨 Color picker confirmed - capturing screenshot");
            
            if (_overlay != null && _isPenMode)
            {
                try
                {
                    // Hide overlay and toolbar temporarily
                    _overlay.Hide();
                    _toolbar?.Hide();
                    
                    // Wait for windows to hide completely
                    System.Threading.Thread.Sleep(150);
                    
                    // Capture clean desktop screenshot
                    System.Diagnostics.Debug.WriteLine("📸 Capturing desktop screenshot...");
                    var screenshot = CaptureScreen();
                    
                    // Set as overlay background
                    _overlay.SetBackground(screenshot);
                    System.Diagnostics.Debug.WriteLine("✅ Screenshot set as background");
                    
                    // Show overlay and toolbar again
                    _overlay.Show();
                    _toolbar?.Show();
                    
                    // Re-apply pen tool and focus
                    _overlay.SetTool(Forms.AnnotationTool.Pen);
                    _overlay.Focus();
                    _overlay.Activate();
                    
                    System.Diagnostics.Debug.WriteLine("✅ Ready to draw on desktop screenshot");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error capturing screenshot: {ex.Message}");
                    
                    // Show overlay anyway
                    _overlay.Show();
                    _toolbar?.Show();
                }
            }
        }

        private System.Windows.Media.Imaging.BitmapSource CaptureScreen()
        {
            // Lấy physical pixel dimensions qua DPI (đồng bộ với OnScreenshotRequested)
            using (var g0 = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
            {
                double dpiX = g0.DpiX / 96.0;
                double dpiY = g0.DpiY / 96.0;
                int physW = (int)(SystemParameters.PrimaryScreenWidth * dpiX);
                int physH = (int)(SystemParameters.PrimaryScreenHeight * dpiY);

                using (var bitmap = new System.Drawing.Bitmap(physW, physH,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size,
                            System.Drawing.CopyPixelOperation.SourceCopy);
                    }

                    // Chuyển sang BitmapSource + Freeze (bắt buộc cho cross-thread safety)
                    var hBitmap = bitmap.GetHbitmap();
                    try
                    {
                        var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                            hBitmap, IntPtr.Zero, System.Windows.Int32Rect.Empty,
                            System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                        bitmapSource.Freeze();
                        return bitmapSource;
                    }
                    finally
                    {
                        DeleteObject(hBitmap);
                    }
                }
            }
        }

        private void OnColorChanged(object? sender, Forms.ColorChangedEventArgs e)
        {
            _overlay?.SetColor(e.Color);
            System.Diagnostics.Debug.WriteLine($"🎨 Color changed to: {e.Color}");
        }

        private void OnThicknessChanged(object? sender, Forms.ThicknessChangedEventArgs e)
        {
            _overlay?.SetThickness(e.Thickness);
            System.Diagnostics.Debug.WriteLine($"📏 Thickness changed to: {e.Thickness}px");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Clear all annotations
        /// </summary>
        public void ClearAnnotations()
        {
            _overlay?.ClearAll();
        }

        /// <summary>
        /// Get annotation statistics
        /// </summary>
        public string GetAnnotationStatistics()
        {
            return _overlay?.GetStatistics() ?? "Overlay not active";
        }

        private void OnOverlayUndoRedoStackChanged(object? sender, EventArgs e)
        {
            if (_toolbar != null && _overlay?.UndoRedoManager != null)
            {
                _toolbar.UpdateUndoRedoButtonsState(_overlay.UndoRedoManager.CanUndo, _overlay.UndoRedoManager.CanRedo);
            }
        }

        #endregion
    }
}
