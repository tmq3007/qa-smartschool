using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfCanvas = System.Windows.Controls.Canvas;
using WpfPrintDialog = System.Windows.Controls.PrintDialog;

namespace QASmartTouch.Services
{
    /// <summary>
    /// Export Service — Xuất canvas bảng trắng thành PDF/PNG/Print
    /// Sử dụng RenderTargetBitmap + XPS (built-in WPF) để tạo PDF-like output
    /// </summary>
    public static class CanvasExportService
    {
        /// <summary>
        /// Xuất toàn bộ canvas thành file PNG chất lượng cao
        /// </summary>
        public static bool ExportToPng(WpfCanvas canvas, string filePath, double dpi = 192)
        {
            try
            {
                if (canvas == null || canvas.ActualWidth <= 0 || canvas.ActualHeight <= 0) return false;

                var renderBitmap = RenderCanvas(canvas, dpi);
                if (renderBitmap == null) return false;

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                using var fs = File.Create(filePath);
                encoder.Save(fs);

                System.Diagnostics.Debug.WriteLine($"[Export] PNG saved: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Export] PNG error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Xuất toàn bộ canvas thành file PDF (sử dụng XPS → PDF concept)
        /// WPF native approach: render to high-res PNG rồi embed vào XPS document
        /// </summary>
        public static bool ExportToPdf(WpfCanvas canvas, string filePath, double dpi = 192)
        {
            try
            {
                if (canvas == null || canvas.ActualWidth <= 0 || canvas.ActualHeight <= 0) return false;

                // Strategy: Export as high-quality PNG (PDF cần thêm thư viện bên ngoài)
                // Tạm thời export PNG chất lượng cao, sau này tích hợp PdfSharp/iTextSharp
                string pngPath = Path.ChangeExtension(filePath, ".png");
                bool success = ExportToPng(canvas, pngPath, 288); // 3x DPI for print quality

                if (success)
                {
                    // Nếu user yêu cầu PDF, tạo XPS (native WPF) 
                    try
                    {
                        ExportToXps(canvas, Path.ChangeExtension(filePath, ".xps"));
                        System.Diagnostics.Debug.WriteLine($"[Export] XPS saved alongside PNG");
                    }
                    catch
                    {
                        // XPS export is optional bonus
                    }
                }

                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Export] PDF error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Xuất canvas thành XPS document (native WPF, printable)
        /// </summary>
        public static bool ExportToXps(WpfCanvas canvas, string filePath)
        {
            try
            {
                if (canvas == null) return false;

                // Create a FixedDocument
                var fixedDoc = new System.Windows.Documents.FixedDocument();
                
                // Create a page with canvas size
                var pageContent = new System.Windows.Documents.PageContent();
                var fixedPage = new System.Windows.Documents.FixedPage
                {
                    Width = canvas.ActualWidth,
                    Height = canvas.ActualHeight
                };

                // Render canvas to image
                var renderBitmap = RenderCanvas(canvas, 192);
                if (renderBitmap == null) return false;

                var image = new System.Windows.Controls.Image
                {
                    Source = renderBitmap,
                    Width = canvas.ActualWidth,
                    Height = canvas.ActualHeight
                };

                fixedPage.Children.Add(image);
                ((System.Windows.Markup.IAddChild)pageContent).AddChild(fixedPage);
                fixedDoc.Pages.Add(pageContent);

                // Write XPS
                if (File.Exists(filePath)) File.Delete(filePath);
                using var xpsDoc = new System.Windows.Xps.Packaging.XpsDocument(
                    filePath, FileAccess.ReadWrite);
                var writer = System.Windows.Xps.Packaging.XpsDocument.CreateXpsDocumentWriter(xpsDoc);
                writer.Write(fixedDoc);

                System.Diagnostics.Debug.WriteLine($"[Export] XPS saved: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Export] XPS error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// In trực tiếp canvas ra máy in
        /// </summary>
        public static bool PrintCanvas(WpfCanvas canvas, string documentTitle = "QA SmartTouch Board")
        {
            try
            {
                if (canvas == null) return false;

                var printDialog = new WpfPrintDialog();
                if (printDialog.ShowDialog() != true) return false;

                // Tạo visual để in
                var renderBitmap = RenderCanvas(canvas, 192);
                if (renderBitmap == null) return false;

                // Tạo visual container cho print
                var printVisual = new DrawingVisual();
                using (var dc = printVisual.RenderOpen())
                {
                    // Tính toán scale để fit trang in
                    var printArea = printDialog.PrintableAreaWidth;
                    var printHeight = printDialog.PrintableAreaHeight;
                    
                    double scaleX = printArea / canvas.ActualWidth;
                    double scaleY = printHeight / canvas.ActualHeight;
                    double scale = Math.Min(scaleX, scaleY);

                    double width = canvas.ActualWidth * scale;
                    double height = canvas.ActualHeight * scale;

                    // Center on page
                    double offsetX = (printArea - width) / 2;
                    double offsetY = (printHeight - height) / 2;

                    dc.DrawImage(renderBitmap, new Rect(offsetX, offsetY, width, height));
                }

                printDialog.PrintVisual(printVisual, documentTitle);

                System.Diagnostics.Debug.WriteLine($"[Export] Print sent: {documentTitle}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Export] Print error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Render canvas thành RenderTargetBitmap
        /// </summary>
        private static RenderTargetBitmap? RenderCanvas(WpfCanvas canvas, double dpi)
        {
            try
            {
                int width = (int)(canvas.ActualWidth * dpi / 96);
                int height = (int)(canvas.ActualHeight * dpi / 96);
                if (width <= 0 || height <= 0) return null;

                var renderBitmap = new RenderTargetBitmap(
                    width, height, dpi, dpi, PixelFormats.Pbgra32);

                // Ensure layout is up to date
                canvas.Measure(new Size(canvas.ActualWidth, canvas.ActualHeight));
                canvas.Arrange(new Rect(new Size(canvas.ActualWidth, canvas.ActualHeight)));

                renderBitmap.Render(canvas);
                return renderBitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Export] Render error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Hiển thị Save dialog và export theo format được chọn
        /// </summary>
        public static void ShowExportDialog(WpfCanvas canvas)
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Xuất bảng trắng",
                Filter = "PNG Image (*.png)|*.png|XPS Document (*.xps)|*.xps|JPEG Image (*.jpg)|*.jpg",
                DefaultExt = ".png",
                FileName = $"SmartTouch_Board_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveDialog.ShowDialog() != true) return;

            bool success;
            string ext = Path.GetExtension(saveDialog.FileName).ToLowerInvariant();

            switch (ext)
            {
                case ".xps":
                    success = ExportToXps(canvas, saveDialog.FileName);
                    break;
                case ".jpg":
                case ".jpeg":
                    success = ExportToJpeg(canvas, saveDialog.FileName);
                    break;
                default:
                    success = ExportToPng(canvas, saveDialog.FileName);
                    break;
            }

            if (success)
            {
                MessageBox.Show(
                    $"Đã xuất thành công!\n\n📁 {saveDialog.FileName}",
                    "Xuất bảng trắng",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    "Xuất thất bại. Vui lòng thử lại.",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Export to JPEG
        /// </summary>
        private static bool ExportToJpeg(WpfCanvas canvas, string filePath, double dpi = 192)
        {
            try
            {
                var renderBitmap = RenderCanvas(canvas, dpi);
                if (renderBitmap == null) return false;

                var encoder = new JpegBitmapEncoder { QualityLevel = 95 };
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                using var fs = File.Create(filePath);
                encoder.Save(fs);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
