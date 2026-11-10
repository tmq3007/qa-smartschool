using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CanvasControl = System.Windows.Controls.Canvas;

namespace QASmartTouch.Services
{
    /// <summary>
    /// Service để capture vùng được chọn trên canvas thành ảnh
    /// </summary>
    public class SelectionCaptureService
    {
        #region Constants

        private const int MaxImageDimension = 2048; // Max width/height for API optimization
        private const int PngQuality = 85;

        #endregion

        #region Public Methods

        /// <summary>
        /// Capture vùng được chọn trên canvas thành PNG byte array
        /// </summary>
        /// <param name="bounds">Bounds của vùng cần capture</param>
        /// <param name="canvas">Canvas chứa nội dung</param>
        /// <returns>PNG byte array</returns>
        public byte[] CaptureSelectionArea(Rect bounds, CanvasControl canvas)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                throw new ArgumentException("Bounds phải có kích thước dương.", nameof(bounds));
            }

            if (canvas == null)
            {
                throw new ArgumentNullException(nameof(canvas));
            }

            try
            {
                // Calculate scale factor if image is too large
                double scaleFactor = CalculateScaleFactor(bounds.Width, bounds.Height);

                // Create RenderTargetBitmap
                int width = (int)(bounds.Width * scaleFactor);
                int height = (int)(bounds.Height * scaleFactor);

                var renderBitmap = new RenderTargetBitmap(
                    width,
                    height,
                    96 * scaleFactor, // DPI X
                    96 * scaleFactor, // DPI Y
                    PixelFormats.Pbgra32);

                // Create DrawingVisual to render the selected area
                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    // Create VisualBrush from canvas
                    var visualBrush = new VisualBrush(canvas)
                    {
                        ViewboxUnits = BrushMappingMode.Absolute,
                        Viewbox = bounds, // Only render the selected area
                        Stretch = Stretch.None
                    };

                    // Draw white background (important for transparent areas)
                    drawingContext.DrawRectangle(
                        Brushes.White,
                        null,
                        new Rect(0, 0, bounds.Width, bounds.Height));

                    // Draw the canvas content
                    drawingContext.DrawRectangle(
                        visualBrush,
                        null,
                        new Rect(0, 0, bounds.Width, bounds.Height));
                }

                // Render to bitmap
                renderBitmap.Render(drawingVisual);

                // Convert to PNG byte array
                var pngBytes = ConvertToPngBytes(renderBitmap);

                System.Diagnostics.Debug.WriteLine($"✅ Captured selection: {width}x{height} px, {pngBytes.Length / 1024} KB");

                return pngBytes;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Capture error: {ex.Message}");
                throw new Exception($"Lỗi khi capture vùng chọn: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Capture vùng được chọn trên canvas thành RenderTargetBitmap
        /// </summary>
        public RenderTargetBitmap CaptureSelectionAreaAsBitmap(Rect bounds, CanvasControl canvas)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                throw new ArgumentException("Bounds phải có kích thước dương.", nameof(bounds));
            }

            if (canvas == null)
            {
                throw new ArgumentNullException(nameof(canvas));
            }

            try
            {
                double scaleFactor = CalculateScaleFactor(bounds.Width, bounds.Height);
                int width = (int)(bounds.Width * scaleFactor);
                int height = (int)(bounds.Height * scaleFactor);

                var renderBitmap = new RenderTargetBitmap(
                    width,
                    height,
                    96 * scaleFactor,
                    96 * scaleFactor,
                    PixelFormats.Pbgra32);

                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    var visualBrush = new VisualBrush(canvas)
                    {
                        ViewboxUnits = BrushMappingMode.Absolute,
                        Viewbox = bounds,
                        Stretch = Stretch.None
                    };

                    drawingContext.DrawRectangle(
                        Brushes.White,
                        null,
                        new Rect(0, 0, bounds.Width, bounds.Height));

                    drawingContext.DrawRectangle(
                        visualBrush,
                        null,
                        new Rect(0, 0, bounds.Width, bounds.Height));
                }

                renderBitmap.Render(drawingVisual);
                return renderBitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Capture bitmap error: {ex.Message}");
                throw new Exception($"Lỗi khi capture vùng chọn thành bitmap: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Capture toàn bộ canvas thành PNG byte array
        /// </summary>
        public byte[] CaptureFullCanvas(CanvasControl canvas)
        {
            if (canvas == null)
            {
                throw new ArgumentNullException(nameof(canvas));
            }

            var bounds = new Rect(0, 0, canvas.ActualWidth, canvas.ActualHeight);
            return CaptureSelectionArea(bounds, canvas);
        }

        /// <summary>
        /// Lưu vùng được chọn ra file PNG
        /// </summary>
        public void SaveSelectionToFile(Rect bounds, CanvasControl canvas, string filePath)
        {
            var pngBytes = CaptureSelectionArea(bounds, canvas);
            File.WriteAllBytes(filePath, pngBytes);
            System.Diagnostics.Debug.WriteLine($"✅ Saved selection to: {filePath}");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Tính scale factor để giảm kích thước ảnh nếu quá lớn
        /// </summary>
        private double CalculateScaleFactor(double width, double height)
        {
            double maxDimension = Math.Max(width, height);
            if (maxDimension <= 0) return 1.0;
            
            // Nếu vùng chọn nhỏ hơn 400px (ví dụ nét chữ/số đơn lẻ 40x120px), tự động scale factor lên
            // để ảnh render sắc nét có kích thước ~400px, giúp OCR nhận dạng chuẩn 100%.
            double targetMinDimension = 400.0;
            if (maxDimension < targetMinDimension)
            {
                double upScale = targetMinDimension / maxDimension;
                System.Diagnostics.Debug.WriteLine($"📐 Upscaling small selection for OCR: {width}x{height} → scale {upScale:F2}x");
                return upScale;
            }

            if (maxDimension <= MaxImageDimension)
            {
                return 1.0; // No scaling needed
            }

            // Scale down to MaxImageDimension if exceeds max limits
            double scaleFactor = MaxImageDimension / maxDimension;
            System.Diagnostics.Debug.WriteLine($"📐 Scaling image: {width}x{height} → {width * scaleFactor}x{height * scaleFactor}");
            
            return scaleFactor;
        }

        /// <summary>
        /// Convert RenderTargetBitmap to PNG byte array
        /// </summary>
        private byte[] ConvertToPngBytes(RenderTargetBitmap bitmap)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (var memoryStream = new MemoryStream())
            {
                encoder.Save(memoryStream);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Convert RenderTargetBitmap to JPEG byte array (smaller size, lossy)
        /// </summary>
        private byte[] ConvertToJpegBytes(RenderTargetBitmap bitmap, int quality = PngQuality)
        {
            var encoder = new JpegBitmapEncoder
            {
                QualityLevel = quality
            };
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (var memoryStream = new MemoryStream())
            {
                encoder.Save(memoryStream);
                return memoryStream.ToArray();
            }
        }

        #endregion
    }
}
