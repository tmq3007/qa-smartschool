using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QASmartTouch.Services;

namespace QASmartTouch.Forms
{
    /// <summary>
    /// Dialog hiển thị danh sách file đã mở gần đây
    /// Cho phép mở lại file, xóa khỏi danh sách, xóa tất cả
    /// </summary>
    public partial class RecentFilesDialog : Window
    {
        /// <summary>
        /// File path được chọn để mở (null nếu user hủy)
        /// </summary>
        public string? SelectedFilePath { get; private set; }

        public RecentFilesDialog()
        {
            InitializeComponent();
            LoadRecentFiles();
        }

        /// <summary>
        /// Load danh sách file gần đây từ RecentFilesService
        /// </summary>
        private void LoadRecentFiles()
        {
            var service = RecentFilesService.Instance;
            
            // Cleanup file đã bị xóa trước khi hiển thị
            service.CleanupMissing();

            var files = service.RecentFiles;

            if (files.Count == 0)
            {
                EmptyState.Visibility = Visibility.Visible;
                FileList.Visibility = Visibility.Collapsed;
                btnClearAll.IsEnabled = false;
            }
            else
            {
                EmptyState.Visibility = Visibility.Collapsed;
                FileList.Visibility = Visibility.Visible;
                FileList.ItemsSource = null;
                FileList.ItemsSource = files;
                btnClearAll.IsEnabled = true;
            }
        }

        #region Event Handlers

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnOpenSelected.IsEnabled = FileList.SelectedItem is RecentFileEntry;
        }

        private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FileList.SelectedItem is RecentFileEntry entry)
            {
                OpenFile(entry);
            }
        }

        private void btnOpenSelected_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedItem is RecentFileEntry entry)
            {
                OpenFile(entry);
            }
        }

        private void btnRemoveFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string filePath)
            {
                RecentFilesService.Instance.RemoveFile(filePath);
                LoadRecentFiles(); // Refresh
            }
        }

        private void btnClearAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Bạn có chắc muốn xóa toàn bộ danh sách bài giảng gần đây?",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                RecentFilesService.Instance.Clear();
                LoadRecentFiles(); // Refresh
            }
        }

        #endregion

        /// <summary>
        /// Mở file và đóng dialog
        /// </summary>
        private void OpenFile(RecentFileEntry entry)
        {
            if (!System.IO.File.Exists(entry.FilePath))
            {
                var result = MessageBox.Show(
                    $"File không tồn tại:\n{entry.FilePath}\n\nXóa khỏi danh sách?",
                    "File không tìm thấy",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    RecentFilesService.Instance.RemoveFile(entry.FilePath);
                    LoadRecentFiles();
                }
                return;
            }

            SelectedFilePath = entry.FilePath;

            // Cập nhật thời gian mở gần nhất
            RecentFilesService.Instance.AddFile(entry.FilePath, entry.DisplayName);

            DialogResult = true;
            Close();
        }
    }
}
