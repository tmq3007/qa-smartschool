using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Xunit;
using QASmartTouch.Managers;
using QASmartTouch.WhiteboardCore.IO;
using Path = System.IO.Path;

namespace QASmartClass.Tests
{
    /// <summary>
    /// Kiểm thử tính năng Mở rộng Canvas (Resize) và Phân lập trạng thái kích thước, hình nền giữa các bảng
    /// Tuân thủ quy chuẩn QC_4.2_BUGFIX_PROTOCOL
    /// </summary>
    public class V42BoardResizeAndIsolationTests
    {
        [Fact]
        public void Test_BoardResize_DoesNotAffectOtherBoards()
        {
            var thread = new Thread(() =>
            {
                // 1. Arrange: Khởi tạo Canvas chuẩn 1920x1080 và BoardManager (Bảng 1)
                var canvas = new Canvas { Width = 1920, Height = 1080 };
                var manager = new BoardManager(canvas);

                Assert.Equal(1, manager.BoardCount);
                Assert.Equal(1920, manager.CurrentBoard.CanvasWidth);
                Assert.Equal(1080, manager.CurrentBoard.CanvasHeight);

                // Tạo thêm Bảng 2
                var board2 = manager.CreateBoard("Bảng 2");
                Assert.NotNull(board2);
                Assert.Equal(1920, board2.CanvasWidth);
                Assert.Equal(1080, board2.CanvasHeight);

                // 2. Act: Chuyển sang Bảng 2 và Mở rộng gấp đôi (3840x2160)
                manager.SwitchBoard(1);
                Assert.Equal(1, manager.CurrentBoardIndex);

                // Mở rộng Bảng 2
                canvas.Width = 3840;
                canvas.Height = 2160;
                manager.CurrentBoard.CanvasWidth = 3840;
                manager.CurrentBoard.CanvasHeight = 2160;

                // 3. Act: Chuyển về Bảng 1
                manager.SwitchBoard(0);

                // 4. Assert: Bảng 1 phải giữ nguyên kích thước chuẩn 1920x1080, KHÔNG bị mở rộng theo
                Assert.Equal(0, manager.CurrentBoardIndex);
                Assert.Equal(1920, canvas.Width);
                Assert.Equal(1080, canvas.Height);
                Assert.Equal(1920, manager.CurrentBoard.CanvasWidth);
                Assert.Equal(1080, manager.CurrentBoard.CanvasHeight);

                // 5. Act & Assert: Chuyển lại Bảng 2, kích thước mở rộng 3840x2160 phải được khôi phục trọn vẹn
                manager.SwitchBoard(1);
                Assert.Equal(1, manager.CurrentBoardIndex);
                Assert.Equal(3840, canvas.Width);
                Assert.Equal(2160, canvas.Height);
                Assert.Equal(3840, manager.CurrentBoard.CanvasWidth);
                Assert.Equal(2160, manager.CurrentBoard.CanvasHeight);
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void Test_NewlyCreatedBoard_AlwaysHasDefaultSize_EvenIfPreviousBoardWasResized()
        {
            var thread = new Thread(() =>
            {
                var canvas = new Canvas { Width = 1920, Height = 1080 };
                var manager = new BoardManager(canvas);

                // Mở rộng Bảng 1 thành 3840x2160
                canvas.Width = 3840;
                canvas.Height = 2160;
                manager.CurrentBoard.CanvasWidth = 3840;
                manager.CurrentBoard.CanvasHeight = 2160;

                // Tạo Bảng 2 mới khi Bảng 1 đang mở rộng
                var board2 = manager.CreateBoard("Bảng 2 mới");
                Assert.NotNull(board2);

                // Chuyển sang Bảng 2
                manager.SwitchBoard(1);

                // Bảng mới tạo phải có kích thước chuẩn 1920x1080
                Assert.Equal(1920, canvas.Width);
                Assert.Equal(1080, canvas.Height);
                Assert.Equal(1920, board2.CanvasWidth);
                Assert.Equal(1080, board2.CanvasHeight);
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void Test_DuplicateBoard_PreservesCustomDimensions()
        {
            var thread = new Thread(() =>
            {
                var canvas = new Canvas { Width = 1920, Height = 1080 };
                var manager = new BoardManager(canvas);

                // Mở rộng Bảng 1 thành 3000x2000
                canvas.Width = 3000;
                canvas.Height = 2000;
                manager.CurrentBoard.CanvasWidth = 3000;
                manager.CurrentBoard.CanvasHeight = 2000;

                // Nhân bản Bảng 1
                var duplicateBoard = manager.DuplicateCurrentBoard();
                Assert.NotNull(duplicateBoard);

                // Kích thước của bản sao phải trùng khớp với bảng gốc
                Assert.Equal(3000, duplicateBoard.CanvasWidth);
                Assert.Equal(2000, duplicateBoard.CanvasHeight);

                // Chuyển sang bản sao
                manager.SwitchBoard(1);
                Assert.Equal(3000, canvas.Width);
                Assert.Equal(2000, canvas.Height);
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void Test_SaveAndLoadLecture_PreservesPerBoardDimensions()
        {
            var thread = new Thread(() =>
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "QASmartTouch_ResizeTest_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);
                string testQascFile = Path.Combine(tempDir, "Lecture_ResizeTest.qasc");

                try
                {
                    // 1. Arrange: Tạo bài giảng với 2 trang có kích thước khác nhau
                    var canvas = new Canvas { Width = 1920, Height = 1080 };
                    var manager = new BoardManager(canvas);

                    // Bảng 1: Chuẩn 1920x1080
                    manager.CurrentBoard.Name = "Trang Chuẩn";
                    manager.CurrentBoard.BackgroundColorHex = "#FFFFFF";
                    manager.CurrentBoard.BackgroundPattern = "lines";

                    // Bảng 2: Mở rộng 3840x2160
                    var board2 = manager.CreateBoard("Trang Mở Rộng");
                    Assert.NotNull(board2);
                    manager.SwitchBoard(1);
                    canvas.Width = 3840;
                    canvas.Height = 2160;
                    manager.CurrentBoard.CanvasWidth = 3840;
                    manager.CurrentBoard.CanvasHeight = 2160;
                    manager.CurrentBoard.BackgroundColorHex = "#1E293B";
                    manager.CurrentBoard.BackgroundPattern = "dots";

                    // 2. Act: Lưu bài giảng
                    bool saved = SaveLoadService.SaveLecture(manager, testQascFile, "Bài Giảng Phân Lập Kích Thước");
                    Assert.True(saved);
                    Assert.True(File.Exists(testQascFile));

                    // 3. Act: Nạp lại bài giảng vào BoardManager mới
                    var loadCanvas = new Canvas { Width = 1920, Height = 1080 };
                    var loadManager = new BoardManager(loadCanvas);

                    var manifest = SaveLoadService.LoadLecture(loadManager, testQascFile);
                    Assert.NotNull(manifest);
                    Assert.Equal(2, loadManager.BoardCount);

                    // 4. Assert: Kiểm tra kích thước và nền từng trang
                    var loadedBoard1 = loadManager.Boards[0];
                    Assert.Equal(1920, loadedBoard1.CanvasWidth);
                    Assert.Equal(1080, loadedBoard1.CanvasHeight);
                    Assert.Equal("#FFFFFF", loadedBoard1.BackgroundColorHex);
                    Assert.Equal("lines", loadedBoard1.BackgroundPattern);

                    var loadedBoard2 = loadManager.Boards[1];
                    Assert.Equal(3840, loadedBoard2.CanvasWidth);
                    Assert.Equal(2160, loadedBoard2.CanvasHeight);
                    Assert.Equal("#1E293B", loadedBoard2.BackgroundColorHex);
                    Assert.Equal("dots", loadedBoard2.BackgroundPattern);

                    // Chuyển sang trang 2 và kiểm tra Canvas kích thước thực tế
                    loadManager.SwitchBoard(1);
                    Assert.Equal(3840, loadCanvas.Width);
                    Assert.Equal(2160, loadCanvas.Height);
                }
                finally
                {
                    if (Directory.Exists(tempDir))
                    {
                        try { Directory.Delete(tempDir, true); } catch { }
                    }
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void Test_DeleteActiveBoard_WhenMultipleBoardsExist_SwitchesToAnotherBoardAndDeletesSuccessfully()
        {
            var thread = new Thread(() =>
            {
                var canvas = new Canvas { Width = 1920, Height = 1080 };
                var manager = new BoardManager(canvas);

                // Tạo thêm Bảng 2
                var board2 = manager.CreateBoard("Bảng 2");
                Assert.NotNull(board2);
                Assert.Equal(2, manager.BoardCount);

                // Đang đứng ở Bảng 1 (Active board, index 0)
                Assert.Equal(0, manager.CurrentBoardIndex);

                // Act: Xóa Bảng 1 khi đang active
                bool deleted = manager.DeleteBoard(0);

                // Assert: Xóa thành công, tự động chuyển sang bảng còn lại
                Assert.True(deleted);
                Assert.Equal(1, manager.BoardCount);
                Assert.Equal(0, manager.CurrentBoardIndex);
                Assert.NotNull(manager.CurrentBoard);
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void Test_DeleteBoard_Fails_WhenOnlyOneBoardExists()
        {
            var thread = new Thread(() =>
            {
                var canvas = new Canvas { Width = 1920, Height = 1080 };
                var manager = new BoardManager(canvas);

                // Chỉ có 1 bảng duy nhất
                Assert.Equal(1, manager.BoardCount);

                // Act & Assert: Không được phép xóa bảng duy nhất còn lại
                bool deleted = manager.DeleteBoard(0);
                Assert.False(deleted);
                Assert.Equal(1, manager.BoardCount);
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
    }
}
