using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace QASmartClass.LearningTools.Models
{
    /// <summary>
    /// Cung cấp ảnh chụp tùy biến bất đồng bộ phục vụ việc đưa nội dung lên bảng trắng SmartScreen.
    /// Dùng cho các công cụ chứa WebView2 hoặc các trò chơi cần lọc bỏ nút bấm điều khiển game.
    /// </summary>
    public interface IWhiteboardCaptureProvider
    {
        Task<BitmapSource?> GetWhiteboardBitmapAsync();
    }
}
