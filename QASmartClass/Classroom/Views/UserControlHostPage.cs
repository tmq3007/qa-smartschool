using System.Windows;
using System.Windows.Controls;

namespace QASmartClass.Classroom.Views
{
    /// <summary>
    /// Trang bao đóng nhẹ (Lightweight Page wrapper) để nhúng bất kỳ UserControl nào vào Navigation Frame
    /// giúp hiển thị mượt mà với tốc độ tức thì O(1) và hỗ trợ caching đầy đủ.
    /// </summary>
    public class UserControlHostPage : Page
    {
        public FrameworkElement HostedControl { get; }

        public UserControlHostPage(FrameworkElement control)
        {
            HostedControl = control;
            Background = System.Windows.Media.Brushes.Transparent;
            Content = control;
        }
    }
}
