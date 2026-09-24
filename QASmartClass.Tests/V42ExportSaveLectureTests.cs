using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Xunit;
using QASmartTouch.Managers;
using QASmartTouch.Services;
using QASmartTouch.WhiteboardCore.IO;
using Path = System.IO.Path;

namespace QASmartClass.Tests
{
    public class V42ExportSaveLectureTests
    {
        [Fact]
        public void Test_SaveLoadService_SaveAndLoad_MultiBoard_Package()
        {
            var thread = new Thread(() =>
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "QASmartTouch_Test_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);
                string testQascFile = Path.Combine(tempDir, "TestLecture.qasc");

                try
                {
                    // 1. Arrange: Khởi tạo BoardManager với 2 bảng
                    var canvas = new Canvas { Width = 800, Height = 600 };
                    var manager = new BoardManager(canvas);

                    // Bảng 1: Thêm hình chữ nhật và đường gấp khúc
                    var rect = new Rectangle
                    {
                        Width = 120,
                        Height = 80,
                        Fill = Brushes.Red,
                        Stroke = Brushes.Black,
                        StrokeThickness = 2
                    };
                    Canvas.SetLeft(rect, 50);
                    Canvas.SetTop(rect, 50);
                    canvas.Children.Add(rect);

                    var polyline = new Polyline
                    {
                        Points = new PointCollection { new Point(10, 10), new Point(40, 50), new Point(90, 20) },
                        Stroke = Brushes.Blue,
                        StrokeThickness = 3
                    };
                    canvas.Children.Add(polyline);

                    // Bảng 2: Tạo bảng mới với cấu hình nền tùy biến
                    var board2 = manager.CreateBoard("Bảng 2: Hình học");
                    Assert.NotNull(board2);
                    board2.BackgroundColorHex = "#1E293B";
                    board2.BackgroundPattern = "grid";
                    board2.LineSpacing = 50;
                    board2.LineOpacity = 25;

                    manager.SwitchBoard(1);
                    var ellipse = new Ellipse
                    {
                        Width = 100,
                        Height = 100,
                        Fill = Brushes.Green
                    };
                    Canvas.SetLeft(ellipse, 200);
                    Canvas.SetTop(ellipse, 150);
                    canvas.Children.Add(ellipse);

                    // 2. Act: Lưu bài giảng thành file .qasc
                    bool saveSuccess = SaveLoadService.SaveLecture(manager, testQascFile, "Tiết học Đại số & Hình học");
                    Assert.True(saveSuccess);
                    Assert.True(File.Exists(testQascFile));
                    Assert.True(new FileInfo(testQascFile).Length > 0);

                    // 3. Act: Nạp lại bài giảng từ file .qasc vào 1 BoardManager mới
                    var newCanvas = new Canvas { Width = 800, Height = 600 };
                    var newManager = new BoardManager(newCanvas);

                    var manifest = SaveLoadService.LoadLecture(newManager, testQascFile);

                    // 4. Assert
                    Assert.NotNull(manifest);
                    Assert.Equal("Tiết học Đại số & Hình học", manifest.Title);
                    Assert.Equal(2, newManager.BoardCount);

                    // Kiểm tra thuộc tính Bảng 1
                    var loadedBoard1 = newManager.Boards[0];
                    Assert.Equal("Bảng 1", loadedBoard1.Name);
                    Assert.NotNull(loadedBoard1.CanvasElements);
                    Assert.True(loadedBoard1.CanvasElements.Count >= 2);

                    // Kiểm tra thuộc tính Bảng 2
                    var loadedBoard2 = newManager.Boards[1];
                    Assert.Equal("Bảng 2: Hình học", loadedBoard2.Name);
                    Assert.Equal("#1E293B", loadedBoard2.BackgroundColorHex);
                    Assert.Equal("grid", loadedBoard2.BackgroundPattern);
                    Assert.Equal(50, loadedBoard2.LineSpacing);
                    Assert.Equal(25, loadedBoard2.LineOpacity);
                    Assert.NotNull(loadedBoard2.CanvasElements);
                    Assert.True(loadedBoard2.CanvasElements.Count >= 1);
                }
                finally
                {
                    try
                    {
                        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                    }
                    catch { }
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void Test_CanvasExportService_ExportImagesToPdf()
        {
            string tempPdfFile = Path.Combine(Path.GetTempPath(), "ExportTest_" + Guid.NewGuid().ToString("N") + ".pdf");

            try
            {
                // Tạo 1 ảnh PNG mẫu dạng byte[]
                byte[] sampleImageBytes;
                using (var ms = new MemoryStream())
                {
                    var bmp = new System.Windows.Media.Imaging.RenderTargetBitmap(200, 150, 96, 96, PixelFormats.Pbgra32);
                    var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bmp));
                    encoder.Save(ms);
                    sampleImageBytes = ms.ToArray();
                }

                var pages = new List<(byte[] ImageBytes, string PageTitle)>
                {
                    (sampleImageBytes, "Trang 1: Khởi động"),
                    (sampleImageBytes, "Trang 2: Kiến thức mới")
                };

                // Act: Xuất PDF qua QuestPDF
                bool success = CanvasExportService.ExportImagesToPdf(pages, tempPdfFile, "Bài giảng thử nghiệm");

                // Assert
                Assert.True(success);
                Assert.True(File.Exists(tempPdfFile));
                Assert.True(new FileInfo(tempPdfFile).Length > 0);

                // Kiểm tra Header tệp có phải PDF (%PDF)
                byte[] headerBytes = new byte[4];
                using (var fs = File.OpenRead(tempPdfFile))
                {
                    fs.Read(headerBytes, 0, 4);
                }
                string headerStr = System.Text.Encoding.ASCII.GetString(headerBytes);
                Assert.Equal("%PDF", headerStr);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPdfFile)) File.Delete(tempPdfFile);
                }
                catch { }
            }
        }

        [Fact]
        public void Test_RecentFilesService_RegistersQascFile()
        {
            string dummyPath = Path.Combine(Path.GetTempPath(), "BaiGiang_Lop9.qasc");
            var service = RecentFilesService.Instance;

            service.AddFile(dummyPath, "Bài giảng Lớp 9");

            Assert.NotEmpty(service.RecentFiles);
            Assert.Equal(dummyPath, service.RecentFiles[0].FilePath);
            Assert.Equal("Bài giảng Lớp 9", service.RecentFiles[0].DisplayName);
            Assert.Equal(".qasc", service.RecentFiles[0].FileType);

            // Cleanup
            service.RemoveFile(dummyPath);
        }

        [Fact]
        public void Test_CanvasExportService_PreservesChalkboardBackground_InPngAndJpeg()
        {
            var thread = new Thread(() =>
            {
                string tempPng = Path.Combine(Path.GetTempPath(), "BgTest_" + Guid.NewGuid().ToString("N") + ".png");
                string tempJpeg = Path.Combine(Path.GetTempPath(), "BgTest_" + Guid.NewGuid().ToString("N") + ".jpg");

                try
                {
                    var canvas = new Canvas { Width = 200, Height = 150 };
                    
                    // Thêm lớp nền bảng với Tag = "BackgroundLayer"
                    var bgRect = new Rectangle
                    {
                        Width = 200,
                        Height = 150,
                        Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3D6D64")),
                        Tag = "BackgroundLayer"
                    };
                    canvas.Children.Add(bgRect);

                    // Thêm phần tử UI hệ thống (giả lập SelectionBox)
                    var sysUi = new Border { Tag = "SystemUI", Width = 50, Height = 50 };
                    canvas.Children.Add(sysUi);

                    // Thêm nét vẽ màu trắng (chữ viết phấn trắng)
                    var chalkText = new Rectangle { Width = 30, Height = 30, Fill = Brushes.White };
                    Canvas.SetLeft(chalkText, 100);
                    Canvas.SetTop(chalkText, 60);
                    canvas.Children.Add(chalkText);

                    // Đo đạc layout
                    canvas.Measure(new Size(200, 150));
                    canvas.Arrange(new Rect(0, 0, 200, 150));
                    canvas.UpdateLayout();

                    Func<UIElement, bool> isSystemPredicate = el => el is Border;

                    // Act 1: Xuất PNG
                    bool pngSuccess = CanvasExportService.ExportToPng(canvas, tempPng, 96, isSystemPredicate);
                    Assert.True(pngSuccess);
                    Assert.True(File.Exists(tempPng));

                    // Act 2: Xuất JPEG
                    bool jpegSuccess = CanvasExportService.ExportToJpeg(canvas, tempJpeg, 96, isSystemPredicate);
                    Assert.True(jpegSuccess);
                    Assert.True(File.Exists(tempJpeg));

                    // Kiểm tra ảnh PNG: Đọc pixel góc (10, 10) để xác nhận nền là màu xanh bảng (#3D6D64), không phải trong suốt hay trắng tinh
                    var pngDecoder = new System.Windows.Media.Imaging.PngBitmapDecoder(
                        new Uri(tempPng, UriKind.Absolute),
                        System.Windows.Media.Imaging.BitmapCreateOptions.None,
                        System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                    var frame = pngDecoder.Frames[0];
                    Assert.Equal(200, frame.PixelWidth);
                    Assert.Equal(150, frame.PixelHeight);

                    byte[] pixels = new byte[4];
                    frame.CopyPixels(new Int32Rect(10, 10, 1, 1), pixels, 4, 0);
                    // BGRA: B = 0x64 (100), G = 0x6D (109), R = 0x3D (61), A = 0xFF (255)
                    byte b = pixels[0];
                    byte g = pixels[1];
                    byte r = pixels[2];
                    byte a = pixels[3];

                    Assert.Equal(255, a); // Không bị trong suốt
                    Assert.Equal(0x3D, r); // Màu đỏ đặc trưng của #3D6D64
                    Assert.Equal(0x6D, g); // Màu lục đặc trưng của #3D6D64
                    Assert.Equal(0x64, b); // Màu lam đặc trưng của #3D6D64
                }
                finally
                {
                    try { if (File.Exists(tempPng)) File.Delete(tempPng); } catch { }
                    try { if (File.Exists(tempJpeg)) File.Delete(tempJpeg); } catch { }
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void Test_Form2_DirtyState_And_UnsavedChangesLogic()
        {
            var thread = new Thread(() =>
            {
                var dashboard = new QASmartTouch.Forms.Form2_MainDashboard();

                // Ban đầu khởi tạo: chưa có thao tác sửa đổi -> HasUnsavedChanges = false
                Assert.False(dashboard.HasUnsavedChanges);
                Assert.False(dashboard.CheckHasUnsavedChanges());

                // Khi đánh dấu dirty
                dashboard.MarkAsDirty();
                Assert.True(dashboard.HasUnsavedChanges);

                // Thêm một nét vẽ/phần tử người dùng
                var userElement = new Rectangle { Width = 50, Height = 50, Fill = Brushes.Blue };
                dashboard.MainInteractiveBoard.Children.Add(userElement);

                // Lúc này CheckHasUnsavedChanges() phải trả về true
                Assert.True(dashboard.CheckHasUnsavedChanges());

                // Khi gọi ClearDirty (hoặc sau khi lưu thành công)
                dashboard.ClearDirty();
                Assert.False(dashboard.HasUnsavedChanges);
                Assert.False(dashboard.CheckHasUnsavedChanges());
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
    }
}
