using System.Linq;
using System.Windows;
using QASmartTouch.Helpers;

namespace QASmartTouch.Shared
{
    /// <summary>
    /// Tiện ích dùng chung (Shared Utility) quản lý việc hiển thị các cửa sổ con (child window)
    /// trong toàn bộ hệ thống QA SmartClass v4.2.
    /// 
    /// Giải quyết lỗi Form con bị chìm xuống dưới cửa sổ chính khi cửa sổ chính có Topmost = true
    /// (ví dụ: khi MainDashboard ở chế độ Fullscreen, hoặc PeriodicTable MainWindow luôn Topmost)
    /// bằng cách tự động thiết lập Owner, kế thừa Topmost, và đảm bảo Focus/Activate cho cửa sổ con.
    /// Đồng thời tự động tích hợp TouchActivationHelper để đảm bảo cú chạm đầu tiên trên màn hình
    /// tương tác (IFP) luôn được kích hoạt ngay lập tức (Zero 2nd tap).
    /// 
    /// Cách sử dụng:
    ///   - Modal:     WindowHelper.ShowChildDialog(childWindow, ownerWindow);
    ///   - Non-Modal: WindowHelper.ShowChildWindow(childWindow, ownerWindow);
    ///   - Nếu không truyền owner, tự động tìm cửa sổ đang Active hoặc Application.MainWindow.
    /// </summary>
    public static class WindowHelper
    {
        /// <summary>
        /// Mở cửa sổ con dạng Modal (ShowDialog) với đầy đủ thiết lập Owner, Topmost và Focus.
        /// Đảm bảo cửa sổ con luôn hiển thị trên cửa sổ chính (Bring to Front) và nhận Focus.
        /// </summary>
        /// <param name="childWindow">Cửa sổ con cần hiển thị</param>
        /// <param name="owner">
        /// Cửa sổ cha (Owner). Nếu null, tự động tìm cửa sổ đang Active hoặc MainWindow.
        /// </param>
        /// <returns>Kết quả DialogResult của cửa sổ con</returns>
        public static bool? ShowChildDialog(Window childWindow, Window owner = null)
        {
            // 1. Tự động xác định Owner nếu chưa truyền vào
            if (owner == null)
            {
                owner = FindActiveOwner();
            }

            // 2. Gán Owner (tránh gán chính nó làm Owner)
            if (owner != null && owner != childWindow)
            {
                childWindow.Owner = owner;

                // Nếu XAML chưa đặt WindowStartupLocation = CenterOwner, tự động set
                if (childWindow.WindowStartupLocation == WindowStartupLocation.Manual)
                {
                    childWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
            }

            // 3. Kế thừa Topmost từ Owner để đảm bảo hiển thị trên cùng
            //    (Quan trọng: Khi MainDashboard ở Fullscreen mode có Topmost = true,
            //     cửa sổ con cũng cần Topmost = true để không bị chèn lên)
            if (owner != null && owner.Topmost)
            {
                childWindow.Topmost = true;
            }

            // 4. QC_4.2_TOUCH_ACTIVATION: Đảm bảo cửa sổ nhận cú chạm đầu tiên trên màn hình tương tác (IFP)
            TouchActivationHelper.ApplyToWindow(childWindow);

            // 5. Đảm bảo Focus và Activate khi cửa sổ con hiển thị
            childWindow.Loaded += ChildWindow_Loaded;

            try
            {
                return childWindow.ShowDialog();
            }
            finally
            {
                // Dọn dẹp event handler để tránh memory leak
                childWindow.Loaded -= ChildWindow_Loaded;
            }
        }

        /// <summary>
        /// Mở cửa sổ con dạng Non-Modal (Show) với đầy đủ thiết lập Owner, Topmost và Focus.
        /// Dùng cho các cửa sổ cần hiển thị song song (không block cửa sổ cha).
        /// </summary>
        /// <param name="childWindow">Cửa sổ con cần hiển thị</param>
        /// <param name="owner">
        /// Cửa sổ cha (Owner). Nếu null, tự động tìm cửa sổ đang Active hoặc MainWindow.
        /// </param>
        public static void ShowChildWindow(Window childWindow, Window owner = null)
        {
            // 1. Tự động xác định Owner nếu chưa truyền vào
            if (owner == null)
            {
                owner = FindActiveOwner();
            }

            // 2. Gán Owner (tránh gán chính nó làm Owner)
            if (owner != null && owner != childWindow)
            {
                childWindow.Owner = owner;

                if (childWindow.WindowStartupLocation == WindowStartupLocation.Manual)
                {
                    childWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
            }

            // 3. Kế thừa Topmost từ Owner
            if (owner != null && owner.Topmost)
            {
                childWindow.Topmost = true;
            }

            // 4. QC_4.2_TOUCH_ACTIVATION: Đảm bảo cửa sổ nhận cú chạm đầu tiên trên màn hình tương tác (IFP)
            TouchActivationHelper.ApplyToWindow(childWindow);

            // 5. Đảm bảo Focus và Activate
            childWindow.Loaded += ChildWindow_Loaded;

            childWindow.Show();
        }

        /// <summary>
        /// Tìm cửa sổ Owner phù hợp nhất trong ứng dụng.
        /// Ưu tiên: Cửa sổ đang Active và Visible → fallback về Application.MainWindow.
        /// </summary>
        /// <returns>Cửa sổ Owner hoặc null nếu không tìm được</returns>
        private static Window FindActiveOwner()
        {
            return Application.Current?.Windows
                        .OfType<Window>()
                        .FirstOrDefault(w => w.IsActive && w.IsVisible)
                    ?? Application.Current?.MainWindow;
        }

        /// <summary>
        /// Handler đảm bảo cửa sổ con nhận Focus và Activate khi vừa hiển thị.
        /// Đặc biệt quan trọng trên các hệ thống bảng tương tác (SMART TOUCH) 
        /// để sự kiện chạm bắt đúng vào cửa sổ con.
        /// </summary>
        private static void ChildWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Window window)
            {
                window.Activate();
                window.Focus();
                window.Dispatcher.BeginInvoke(new System.Action(() =>
                {
                    try
                    {
                        window.Activate();
                        window.Focus();
                    }
                    catch { }
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
        }
    }
}
