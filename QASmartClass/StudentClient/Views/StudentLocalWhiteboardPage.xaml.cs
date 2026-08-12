using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QASmartTouch;
using Serilog;

namespace QASmartClass.StudentClient.Views
{
    public partial class StudentLocalWhiteboardPage : Page
    {
        private bool _isSubmitting = false;
        private readonly Stack<QASmartClass.Services.Canvas.IUndoableCommand> _undoStack = new Stack<QASmartClass.Services.Canvas.IUndoableCommand>();
        private readonly Stack<QASmartClass.Services.Canvas.IUndoableCommand> _redoStack = new Stack<QASmartClass.Services.Canvas.IUndoableCommand>();
        private bool _isUndoRedoing = false;
        
        // ✅ Phase C: Giới hạn stack size tránh rò rỉ bộ nhớ khi học sinh vẽ liên tục
        // 200 actions đủ cho 1 bài vẽ dài — mỗi IUndoableCommand chứa ref Stroke (~200 bytes)
        private const int MAX_STUDENT_UNDO_LEVELS = 200;

        public StudentLocalWhiteboardPage()
        {
            InitializeComponent();
            ApplyDefaultPen();

            if (localInkCanvas != null)
            {
                localInkCanvas.Strokes.StrokesChanged += Strokes_StrokesChanged;
            }
            this.PreviewKeyDown += StudentLocalWhiteboardPage_PreviewKeyDown;
        }

        private void Strokes_StrokesChanged(object sender, StrokeCollectionChangedEventArgs e)
        {
            if (_isUndoRedoing) return;

            bool hasRealDrawing = false;

            if (e.Added.Count > 0)
            {
                foreach (var stroke in e.Added)
                {
                    _undoStack.Push(new QASmartClass.Services.Canvas.AddStrokeCommand(localInkCanvas, stroke));
                    
                    // Check if the stroke is a deliberate drawing (not an accidental palm/finger touch)
                    double minX = double.MaxValue, minY = double.MaxValue;
                    double maxX = double.MinValue, maxY = double.MinValue;
                    foreach (var pt in stroke.StylusPoints)
                    {
                        minX = Math.Min(minX, pt.X);
                        minY = Math.Min(minY, pt.Y);
                        maxX = Math.Max(maxX, pt.X);
                        maxY = Math.Max(maxY, pt.Y);
                    }
                    double width = (stroke.StylusPoints.Count > 0) ? (maxX - minX) : 0;
                    double height = (stroke.StylusPoints.Count > 0) ? (maxY - minY) : 0;
                    bool isAccidental = stroke.StylusPoints.Count <= 3 && width <= 5 && height <= 5;
                    if (!isAccidental)
                    {
                        hasRealDrawing = true;
                    }
                }
            }
            if (e.Removed.Count > 0)
            {
                _undoStack.Push(new QASmartClass.Services.Canvas.EraseStrokesCommand(localInkCanvas, e.Removed));
                hasRealDrawing = true; // Erasing is always a deliberate action
            }

            if (hasRealDrawing)
            {
                _redoStack.Clear();
            }
            
            // ✅ Phase C: Trim undo stack nếu vượt quá giới hạn
            if (_undoStack.Count > MAX_STUDENT_UNDO_LEVELS)
            {
                var temp = _undoStack.ToList();
                temp.RemoveAt(temp.Count - 1); // Xóa action cũ nhất (đáy stack = index cuối trong List)
                _undoStack.Clear();
                // Re-push theo thứ tự đúng: temp[0] = đỉnh stack → push cuối → trở lại đỉnh
                for (int i = temp.Count - 1; i >= 0; i--)
                {
                    _undoStack.Push(temp[i]);
                }
            }

            UpdateUndoRedoButtons();
        }

        private void StudentLocalWhiteboardPage_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
            {
                if (e.Key == System.Windows.Input.Key.Z)
                {
                    PerformUndo();
                    e.Handled = true;
                }
                else if (e.Key == System.Windows.Input.Key.Y)
                {
                    PerformRedo();
                    e.Handled = true;
                }
            }
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            PerformUndo();
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            PerformRedo();
        }

        private void PerformUndo()
        {
            if (localInkCanvas != null && _undoStack.Count > 0)
            {
                _isUndoRedoing = true;
                var command = _undoStack.Pop();
                command.Unexecute();
                _redoStack.Push(command);
                _isUndoRedoing = false;
                UpdateUndoRedoButtons();
            }
        }

        private void PerformRedo()
        {
            if (localInkCanvas != null && _redoStack.Count > 0)
            {
                _isUndoRedoing = true;
                var command = _redoStack.Pop();
                command.Execute();
                _undoStack.Push(command);
                _isUndoRedoing = false;
                UpdateUndoRedoButtons();
            }
        }

        private void UpdateUndoRedoButtons()
        {
            if (btnUndo != null) btnUndo.IsEnabled = _undoStack.Count > 0;
            if (btnRedo != null) btnRedo.IsEnabled = _redoStack.Count > 0;
        }

        private void ApplyDefaultPen()
        {
            if (localInkCanvas != null)
            {
                localInkCanvas.DefaultDrawingAttributes.Color = Colors.Black;
                localInkCanvas.DefaultDrawingAttributes.Width = 3;
                localInkCanvas.DefaultDrawingAttributes.Height = 3;
                localInkCanvas.DefaultDrawingAttributes.FitToCurve = true;
                localInkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                localInkCanvas.EditingModeInverted = InkCanvasEditingMode.EraseByPoint;
                localInkCanvas.EraserShape = new EllipseStylusShape(16, 16);
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (localInkCanvas != null)
            {
                localInkCanvas.Strokes.Clear();
            }
        }

        private void Color_Checked(object sender, RoutedEventArgs e)
        {
            if (localInkCanvas == null || !(sender is RadioButton radio) || radio.Tag == null)
            {
                return;
            }

            string colorName = radio.Tag.ToString();
            switch (colorName)
            {
                case "Black":
                    localInkCanvas.DefaultDrawingAttributes.Color = Colors.Black;
                    break;
                case "Blue":
                    localInkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(37, 99, 235); // Blue 600
                    break;
                case "Red":
                    localInkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(220, 38, 38); // Red 600
                    break;
                case "Green":
                    localInkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(22, 163, 74); // Green 600
                    break;
                case "Yellow":
                    localInkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(234, 179, 8); // Yellow 500
                    break;
                case "Orange":
                    localInkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(249, 115, 22); // Orange 500
                    break;
                case "Purple":
                    localInkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(168, 85, 247); // Purple 500
                    break;
                case "Pink":
                    localInkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(236, 72, 153); // Pink 500
                    break;
            }

            // Tự động chuyển lại chế độ Bút vẽ nếu học sinh đang ở chế độ Tẩy mà bấm chọn màu mới
            if (localInkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint && btnPenMode != null)
            {
                btnPenMode.IsChecked = true;
            }
        }

        private void Size_Checked(object sender, RoutedEventArgs e)
        {
            if (localInkCanvas == null || !(sender is RadioButton radio) || radio.Tag == null)
            {
                return;
            }

            double size = 3.0;
            if (radio.Tag is double dSize)
            {
                size = dSize;
            }
            else if (double.TryParse(radio.Tag.ToString(), out double parsedSize))
            {
                size = parsedSize;
            }

            localInkCanvas.DefaultDrawingAttributes.Width = size;
            localInkCanvas.DefaultDrawingAttributes.Height = size;

            // Tính toán kích thước tẩy tối ưu: tối thiểu là 16px, hoặc gấp 2.5 lần nét bút vẽ
            double eraserSize = Math.Max(size * 2.5, 16.0);
            localInkCanvas.EraserShape = new EllipseStylusShape(eraserSize, eraserSize);
        }

        private void PenMode_Checked(object sender, RoutedEventArgs e)
        {
            if (localInkCanvas != null)
            {
                localInkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            }
        }

        private void EraserMode_Checked(object sender, RoutedEventArgs e)
        {
            if (localInkCanvas != null)
            {
                localInkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
            }
        }

        private async void SubmitToTeacher_Click(object sender, RoutedEventArgs e)
        {
            if (_isSubmitting) return;
            _isSubmitting = true;
            var submitButton = sender as Button;
            if (submitButton != null) submitButton.IsEnabled = false;
            var originalCursor = this.Cursor;
            this.Cursor = System.Windows.Input.Cursors.Wait;

            try
            {
                // 1. Kiểm tra mạng và kết nối WebSocket trước khi xử lý hình ảnh
                App app = (App)Application.Current;
                if (app.StudentNetwork == null || !app.StudentNetwork.IsConnected)
                {
                    MessageBox.Show("Mất kết nối mạng tới lớp học! Không thể gửi bài vẽ lúc này.", 
                                    "Lỗi kết nối", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (localInkCanvas.Strokes.Count == 0)
                {
                    MessageBox.Show("Bảng vẽ hiện tại đang trống. Em hãy vẽ gì đó trước khi gửi nhé!", 
                                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 2. Chụp InkCanvas thành RenderTargetBitmap
                int originalWidth = (int)localInkCanvas.ActualWidth;
                int originalHeight = (int)localInkCanvas.ActualHeight;

                if (originalWidth <= 0 || originalHeight <= 0)
                {
                    MessageBox.Show("Không thể chụp giao diện bảng vẽ vào lúc này. Hãy thử lại.", 
                                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                RenderTargetBitmap originalBmp = new RenderTargetBitmap(originalWidth, originalHeight, 96d, 96d, PixelFormats.Pbgra32);
                originalBmp.Render(localInkCanvas);

                // 3. Tính toán tỷ lệ co ảnh tối ưu (Downscale) để tránh nghẽn băng thông truyền tải
                int targetWidth = originalWidth;
                int targetHeight = originalHeight;
                double maxDimension = 1280.0;

                if (originalWidth > maxDimension || originalHeight > maxDimension)
                {
                    double scale = Math.Min(maxDimension / originalWidth, maxDimension / originalHeight);
                    targetWidth = (int)(originalWidth * scale);
                    targetHeight = (int)(originalHeight * scale);
                }

                // 4. Render lại ảnh co giãn bằng DrawingVisual (tối ưu hóa phần cứng, tránh rò rỉ RAM)
                DrawingVisual visual = new DrawingVisual();
                using (DrawingContext context = visual.RenderOpen())
                {
                    context.DrawImage(originalBmp, new Rect(0, 0, targetWidth, targetHeight));
                }

                RenderTargetBitmap scaledBmp = new RenderTargetBitmap(targetWidth, targetHeight, 96d, 96d, PixelFormats.Pbgra32);
                scaledBmp.Render(visual);

                // 5. Nén ảnh chất lượng JPEG 80% (tối ưu hóa dung lượng)
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = 80;
                encoder.Frames.Add(BitmapFrame.Create(scaledBmp));

                byte[] imageBytes;
                using (MemoryStream stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    imageBytes = stream.ToArray();
                }

                // 6. Mã hóa sang Base64
                string base64String = Convert.ToBase64String(imageBytes);

                // 7. Gửi gói tin qua mạng
                string studentId = app.StudentNetwork.StudentCode ?? "HS_UNKNOWN";
                string cmd = $"SUBMIT_WHITEBOARD|{studentId}|{base64String}";
                await app.StudentNetwork.SendAsync(cmd);

                Log.Information("Student Whiteboard submitted: {Bytes} bytes as base64 (approx {Len} chars)", imageBytes.Length, base64String.Length);

                MessageBox.Show("Đã gửi bài vẽ của em lên màn hình giáo viên thành công!", 
                                "Gửi bài thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi xảy ra khi gửi bài vẽ học sinh");
                MessageBox.Show("Có lỗi xảy ra khi nộp bài vẽ: " + ex.Message, "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                this.Cursor = originalCursor;
                if (submitButton != null) submitButton.IsEnabled = true;
                _isSubmitting = false;
            }
        }
    }
}
