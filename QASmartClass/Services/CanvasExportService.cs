using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QASmartTouch.Managers;
using WpfCanvas = System.Windows.Controls.Canvas;
using WpfPrintDialog = System.Windows.Controls.PrintDialog;
using Size = System.Windows.Size;

namespace QASmartTouch.Services
{
    /// <summary>
    /// Export Service — Xuất canvas bảng trắng thành PDF (QuestPDF) / PNG / JPEG / Print
    /// Hỗ trợ xuất 1 trang hoặc toàn bộ các trang của BoardManager
    /// </summary>
    public static class CanvasExportService
    {
        /// <summary>
        /// Xuất toàn bộ canvas thành file PNG chất lượng cao
        /// </summary>
        public static bool ExportToPng(WpfCanvas canvas, string filePath, double dpi = 192, Func<UIElement, bool>? isSystemElementPredicate = null)
        {
            try
            {
                var bytes = CaptureCanvasToImageBytes(canvas, dpi, isSystemElementPredicate, isJpeg: false);
                if (bytes == null) return false;

                File.WriteAllBytes(filePath, bytes);
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
        /// Xuất toàn bộ canvas thành file JPEG chất lượng cao
        /// </summary>
        public static bool ExportToJpeg(WpfCanvas canvas, string filePath, double dpi = 192, Func<UIElement, bool>? isSystemElementPredicate = null)
        {
            try
            {
                var bytes = CaptureCanvasToImageBytes(canvas, dpi, isSystemElementPredicate, isJpeg: true);
                if (bytes == null) return false;

                File.WriteAllBytes(filePath, bytes);
                System.Diagnostics.Debug.WriteLine($"[Export] JPEG saved: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Export] JPEG error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Chụp Canvas thành mảng byte hình ảnh (PNG hoặc JPEG) chất lượng cao, tạm ẩn System UI controls
        /// Bảo toàn 100% lớp nền bảng (BackgroundLayer)
        /// </summary>
        public static byte[]? CaptureCanvasToImageBytes(
            WpfCanvas canvas, 
            double dpi = 192, 
            Func<UIElement, bool>? isSystemElementPredicate = null,
            bool isJpeg = false)
        {
            try
            {
                if (canvas == null || canvas.ActualWidth <= 0 || canvas.ActualHeight <= 0) return null;

                // Tạm ẩn các control UI hệ thống (SelectionBox, ContextToolbar, EraserPreview...)
                // ✅ RÀNG BUỘC SỐNG CÒN (QC_4.2_BUGFIX_PROTOCOL):
                // Tuyệt đối KHÔNG ẩn lớp nền bảng (BackgroundLayer) để bảo toàn màu nền và lưới ô ly khi xuất ảnh/PDF!
                var hiddenElements = new List<(UIElement element, Visibility original)>();
                foreach (UIElement child in canvas.Children)
                {
                    if (child is FrameworkElement fe && fe.Tag?.ToString() == "BackgroundLayer")
                    {
                        continue;
                    }

                    if (isSystemElementPredicate != null && isSystemElementPredicate(child) && child.Visibility == Visibility.Visible)
                    {
                        hiddenElements.Add((child, child.Visibility));
                        child.Visibility = Visibility.Hidden;
                    }
                }

                try
                {
                    canvas.UpdateLayout();

                    int width = (int)(canvas.ActualWidth * dpi / 96.0);
                    int height = (int)(canvas.ActualHeight * dpi / 96.0);
                    if (width <= 0 || height <= 0) return null;

                    var renderBitmap = new RenderTargetBitmap(width, height, dpi, dpi, PixelFormats.Pbgra32);

                    // Kiểm tra xem canvas có BackgroundLayer hiển thị hay không
                    bool hasVisibleBgLayer = false;
                    foreach (UIElement child in canvas.Children)
                    {
                        if (child is FrameworkElement fe && fe.Tag?.ToString() == "BackgroundLayer" && child.Visibility == Visibility.Visible)
                        {
                            hasVisibleBgLayer = true;
                            break;
                        }
                    }

                    // Nếu không có BackgroundLayer và canvas.Background là transparent/null -> vẽ nền mặc định #3D6D64 trước khi vẽ canvas để tránh trong suốt
                    if (!hasVisibleBgLayer && (canvas.Background == null || canvas.Background == Brushes.Transparent))
                    {
                        var bgVisual = new DrawingVisual();
                        using (var dc = bgVisual.RenderOpen())
                        {
                            var bgBrush = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#3D6D64"));
                            dc.DrawRectangle(bgBrush, null, new Rect(0, 0, canvas.ActualWidth, canvas.ActualHeight));
                        }
                        renderBitmap.Render(bgVisual);
                    }

                    renderBitmap.Render(canvas);

                    BitmapEncoder encoder = isJpeg
                        ? new JpegBitmapEncoder { QualityLevel = 95 }
                        : new PngBitmapEncoder();

                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                    using var ms = new MemoryStream();
                    encoder.Save(ms);
                    return ms.ToArray();
                }
                finally
                {
                    // Khôi phục hiển thị
                    foreach (var (element, original) in hiddenElements)
                    {
                        element.Visibility = original;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Export] Capture error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Chụp Canvas thành mảng byte PNG chất lượng cao, tạm ẩn System UI controls
        /// </summary>
        public static byte[]? CaptureCanvasToPngBytes(WpfCanvas canvas, double dpi = 192, Func<UIElement, bool>? isSystemElementPredicate = null)
        {
            return CaptureCanvasToImageBytes(canvas, dpi, isSystemElementPredicate, isJpeg: false);
        }

        /// <summary>
        /// Xuất canvas hiện tại thành file PDF chuẩn A4 Ngang (QuestPDF)
        /// </summary>
        public static bool ExportToPdf(WpfCanvas canvas, string filePath, string lectureTitle = "QA SmartClass", double dpi = 192, Func<UIElement, bool>? isSystemElementPredicate = null)
        {
            try
            {
                var imageBytes = CaptureCanvasToPngBytes(canvas, dpi, isSystemElementPredicate);
                if (imageBytes == null) return false;

                return ExportImagesToPdf(new List<(byte[] ImageBytes, string PageTitle)> { (imageBytes, "Bảng trắng") }, filePath, lectureTitle);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Export] PDF error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Xuất danh sách các ảnh chụp thành file PDF chuẩn A4 Ngang qua QuestPDF
        /// </summary>
        public static bool ExportImagesToPdf(List<(byte[] ImageBytes, string PageTitle)> pages, string filePath, string lectureTitle = "QA SmartClass")
        {
            if (pages == null || pages.Count == 0 || string.IsNullOrWhiteSpace(filePath)) return false;

            try
            {
                QuestPDF.Settings.License = LicenseType.Community;

                var doc = Document.Create(container =>
                {
                    int totalPages = pages.Count;
                    int pageIndex = 1;

                    foreach (var (imageBytes, pageTitle) in pages)
                    {
                        int currentPage = pageIndex;
                        string title = !string.IsNullOrWhiteSpace(pageTitle) ? pageTitle : $"Trang {currentPage}";

                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4.Landscape());
                            page.Margin(12, Unit.Point);

                            // Header
                            page.Header().PaddingBottom(4, Unit.Point).Row(row =>
                            {
                                row.RelativeItem().Text(lectureTitle).Bold().FontSize(11).FontColor(QuestPDF.Helpers.Colors.Grey.Darken3);
                                row.RelativeItem().AlignRight().Text($"{title} ({currentPage}/{totalPages})").FontSize(9).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                            });

                            // Content: Fit canvas image into A4 landscape
                            page.Content().Image(imageBytes).FitArea();

                            // Footer
                            page.Footer().PaddingTop(4, Unit.Point).Row(row =>
                            {
                                row.RelativeItem().Text($"Xuất ngày {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(QuestPDF.Helpers.Colors.Grey.Lighten1);
                                row.RelativeItem().AlignRight().Text("QA SmartClass - Bảng vẽ tương tác").FontSize(8).FontColor(QuestPDF.Helpers.Colors.Grey.Lighten1);
                            });
                        });

                        pageIndex++;
                    }
                });

                doc.GeneratePdf(filePath);
                System.Diagnostics.Debug.WriteLine($"[Export] ✅ PDF đã xuất thành công: {filePath} ({pages.Count} trang)");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Export] ❌ Lỗi xuất PDF qua QuestPDF: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Xuất tất cả các trang của BoardManager thành 1 file PDF A4 Ngang ghép trang
        /// </summary>
        public static bool ExportBoardsToPdf(BoardManager boardManager, WpfCanvas canvas, string filePath, string lectureTitle = "QA SmartClass", double dpi = 192, Action<int, int>? progressCallback = null)
        {
            if (boardManager == null || canvas == null) return false;

            int originalIndex = boardManager.CurrentBoardIndex;
            var capturedPages = new List<(byte[] ImageBytes, string PageTitle)>();

            try
            {
                int total = boardManager.BoardCount;
                for (int i = 0; i < total; i++)
                {
                    progressCallback?.Invoke(i + 1, total);

                    if (boardManager.CurrentBoardIndex != i)
                    {
                        boardManager.SwitchBoard(i);
                    }

                    canvas.UpdateLayout();

                    var bytes = CaptureCanvasToPngBytes(canvas, dpi, boardManager.IsSystemElement);
                    if (bytes != null)
                    {
                        capturedPages.Add((bytes, boardManager.Boards[i].Name));
                    }
                }

                if (capturedPages.Count == 0) return false;

                return ExportImagesToPdf(capturedPages, filePath, lectureTitle);
            }
            finally
            {
                if (boardManager.CurrentBoardIndex != originalIndex)
                {
                    boardManager.SwitchBoard(originalIndex);
                }
            }
        }

        /// <summary>
        /// Xuất tất cả các trang của BoardManager thành các file ảnh riêng lẻ trong thư mục
        /// </summary>
        public static bool ExportBoardsToImages(BoardManager boardManager, WpfCanvas canvas, string outputDirectory, string baseFileName, string format = "png", double dpi = 192, Action<int, int>? progressCallback = null)
        {
            if (boardManager == null || canvas == null || string.IsNullOrWhiteSpace(outputDirectory)) return false;

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            int originalIndex = boardManager.CurrentBoardIndex;
            bool isJpeg = format.Equals("jpg", StringComparison.OrdinalIgnoreCase) || format.Equals("jpeg", StringComparison.OrdinalIgnoreCase);
            string ext = isJpeg ? ".jpg" : ".png";

            try
            {
                int total = boardManager.BoardCount;
                for (int i = 0; i < total; i++)
                {
                    progressCallback?.Invoke(i + 1, total);

                    if (boardManager.CurrentBoardIndex != i)
                    {
                        boardManager.SwitchBoard(i);
                    }

                    canvas.UpdateLayout();

                    string fileName = $"{baseFileName}_Trang_{i + 1}{ext}";
                    string filePath = Path.Combine(outputDirectory, fileName);

                    if (isJpeg)
                    {
                        ExportToJpeg(canvas, filePath, dpi, boardManager.IsSystemElement);
                    }
                    else
                    {
                        ExportToPng(canvas, filePath, dpi, boardManager.IsSystemElement);
                    }
                }

                return true;
            }
            finally
            {
                if (boardManager.CurrentBoardIndex != originalIndex)
                {
                    boardManager.SwitchBoard(originalIndex);
                }
            }
        }

        /// <summary>
        /// Xuất 1 trang đơn lẻ sang file ảnh PNG hoặc JPEG
        /// </summary>
        public static bool ExportSingleBoardToImage(WpfCanvas canvas, string filePath, string format = "png", double dpi = 192, Func<UIElement, bool>? isSystemElementPredicate = null)
        {
            bool isJpeg = format.Equals("jpg", StringComparison.OrdinalIgnoreCase) || format.Equals("jpeg", StringComparison.OrdinalIgnoreCase);
            if (isJpeg)
            {
                return ExportToJpeg(canvas, filePath, dpi, isSystemElementPredicate);
            }
            else
            {
                return ExportToPng(canvas, filePath, dpi, isSystemElementPredicate);
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
    }
}
