using System.Windows;

namespace QASmartClass.Classroom.Helpers
{
    /// <summary>
    /// Chuẩn hóa 331 lần MessageBox.Show(...) rải rác thành API nhất quán.
    /// </summary>
    internal static class ClassroomDialog
    {
        /// <summary>YesNo — trả về true nếu người dùng chọn Yes.</summary>
        public static bool Confirm(string message, string title = "Xác nhận")
            => MessageBox.Show(message, title,
               MessageBoxButton.YesNo,
               MessageBoxImage.Question)
               == MessageBoxResult.Yes;

        /// <summary>YesNoCancel — trả về MessageBoxResult.</summary>
        public static MessageBoxResult ConfirmCancel(string message, string title = "Xác nhận")
            => MessageBox.Show(message, title,
               MessageBoxButton.YesNoCancel,
               MessageBoxImage.Warning);

        /// <summary>Hộp thoại lỗi.</summary>
        public static void Error(string message, string title = "Lỗi hệ thống")
            => MessageBox.Show(message, title,
               MessageBoxButton.OK,
               MessageBoxImage.Error);

        /// <summary>Hộp thoại thông tin.</summary>
        public static void Info(string message, string title = "Thông báo")
            => MessageBox.Show(message, title,
               MessageBoxButton.OK,
               MessageBoxImage.Information);

        /// <summary>Hộp thoại cảnh báo.</summary>
        public static void Warn(string message, string title = "Cảnh báo")
            => MessageBox.Show(message, title,
               MessageBoxButton.OK,
               MessageBoxImage.Warning);

        /// <summary>
        /// Thông báo "phiên chưa bắt đầu" chuẩn — dùng ở nhiều trang.
        /// </summary>
        public static void SessionNotStarted()
            => Warn("Vui lòng bắt đầu lớp học trước khi thực hiện thao tác này.",
                    "Chưa bắt đầu lớp");

        /// <summary>
        /// Thông báo "học sinh offline" — dùng ở context menu trang ClassroomPage.
        /// </summary>
        public static void StudentOffline(string studentName = "")
        {
            var name = string.IsNullOrEmpty(studentName) ? "Học sinh" : studentName;
            Warn($"{name} đang offline. Không thể thực hiện thao tác từ xa.",
                 "Học sinh offline");
        }
    }
}
