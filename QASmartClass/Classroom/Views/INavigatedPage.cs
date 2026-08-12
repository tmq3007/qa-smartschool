using System.Threading.Tasks;

namespace QASmartClass.Classroom.Views
{
    /// <summary>
    /// Giao diện dành cho các Page con cần thực hiện nạp hoặc cập nhật dữ liệu 
    /// một cách bất đồng bộ mỗi khi được điều hướng tới (đặc biệt khi load từ Cache).
    /// </summary>
    public interface INavigatedPage
    {
        /// <summary>
        /// Kích hoạt khi trang được điều hướng tới.
        /// </summary>
        Task OnNavigatedToAsync();
    }
}
