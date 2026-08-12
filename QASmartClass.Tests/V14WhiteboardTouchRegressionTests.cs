using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;
using QASmartTouch.Utilities;

namespace QASmartClass.Tests
{
    /// <summary>
    /// Các bài kiểm thử tự động phòng ngừa tái phát lỗi chặn sự kiện cảm ứng/chuột trên Bảng trắng (Whiteboard).
    /// </summary>
    public class V14WhiteboardTouchRegressionTests
    {
        [Fact]
        public void InputValidationHelper_ShouldReturnTrue_WhenSourceIsInsideButton()
        {
            // Chạy trong luồng STA (WPF yêu cầu luồng STA để khởi tạo và thao tác với UI elements)
            var thread = new System.Threading.Thread(() =>
            {
                // Arrange (Chuẩn bị cấu trúc cây giao diện)
                var canvas = new Canvas();
                var button = new Button();
                var textBlock = new TextBlock();
                
                button.Content = textBlock;
                canvas.Children.Add(button);

                // Act (Kiểm tra xem phần tử TextBlock nằm trong Button có được nhận diện đúng không)
                bool result = InputValidationHelper.IsEventFromInteractiveControl(textBlock, canvas);

                // Assert (Đảm bảo trả về true)
                Assert.True(result, "Phần tử con nằm trong ButtonBase phải được nhận diện là control tương tác.");
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void InputValidationHelper_ShouldReturnFalse_WhenSourceIsDirectCanvasChild()
        {
            var thread = new System.Threading.Thread(() =>
            {
                // Arrange (Chuẩn bị cấu trúc cây giao diện)
                var canvas = new Canvas();
                var border = new Border();
                canvas.Children.Add(border);

                // Act (Kiểm tra phần tử Border nằm trực tiếp trên Canvas)
                bool result = InputValidationHelper.IsEventFromInteractiveControl(border, canvas);

                // Assert (Đảm bảo trả về false)
                Assert.False(result, "Phần tử trực tiếp trên canvas (không thuộc Button) phải trả về false để cho phép vẽ.");
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
    }
}
