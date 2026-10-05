using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QASmartTouch.Forms
{
    public partial class Form5_2_FileDialog : Window
    {
        private string _currentPath;
        private List<string> _navigationHistory;
        private int _historyIndex;
        private string _selectedFilePath;

        public string SelectedFilePath => _selectedFilePath;
        public List<string> SelectedFilePaths { get; private set; }
        public bool AllowMultipleSelection { get; set; }
        public string FileFilter { get; set; } = "*.*";

        public Form5_2_FileDialog()
        {
            InitializeComponent();
            _navigationHistory = new List<string>();
            _historyIndex = -1;
            SelectedFilePaths = new List<string>();

            // Initialize with Documents folder
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            NavigateTo(documentsPath);
        }

        private void NavigateTo(string path)
        {
            if (!Directory.Exists(path))
            {
                MessageBox.Show("Thư mục không tồn tại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _currentPath = path;
            txtPath.Text = path;

            // Add to history
            if (_historyIndex < _navigationHistory.Count - 1)
            {
                _navigationHistory.RemoveRange(_historyIndex + 1, _navigationHistory.Count - _historyIndex - 1);
            }
            _navigationHistory.Add(path);
            _historyIndex = _navigationHistory.Count - 1;

            LoadFiles();
        }

        private void LoadFiles()
        {
            panelFiles.Children.Clear();

            try
            {
                // Get directories
                string[] directories = Directory.GetDirectories(_currentPath);
                foreach (string dir in directories)
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(dir);
                    AddFolderItem(dirInfo.Name, dir);
                }

                // Get files
                string[] files = Directory.GetFiles(_currentPath);
                foreach (string file in files)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    if (MatchesFilter(fileInfo.Extension))
                    {
                        AddFileItem(fileInfo.Name, file, FormatFileSize(fileInfo.Length));
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Không có quyền truy cập thư mục này!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool MatchesFilter(string extension)
        {
            if (FileFilter == "*.*") return true;

            string[] filters = FileFilter.Split(';');
            foreach (string filter in filters)
            {
                string ext = filter.Trim().Replace("*", "");
                if (extension.Equals(ext, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private void AddFolderItem(string name, string fullPath)
        {
            Border border = new Border
            {
                Background = System.Windows.Media.Brushes.LightGray,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 5),
                Cursor = Cursors.Hand,
                Tag = fullPath
            };

            Grid grid = new Grid();

            StackPanel stack = new StackPanel { Orientation = Orientation.Horizontal };
            TextBlock icon = new TextBlock
            {
                Text = "📁",
                FontSize = 16,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock text = new TextBlock
            {
                Text = name,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            stack.Children.Add(icon);
            stack.Children.Add(text);

            TextBlock typeText = new TextBlock
            {
                Text = "Thư mục",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            grid.Children.Add(stack);
            grid.Children.Add(typeText);
            border.Child = grid;

            border.MouseLeftButtonDown += (s, e) =>
            {
                if (s is Border b && b.Tag is string path)
                {
                    NavigateTo(path);
                }
            };

            panelFiles.Children.Add(border);
        }

        private void AddFileItem(string name, string fullPath, string size)
        {
            Border border = new Border
            {
                Background = System.Windows.Media.Brushes.White,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 5),
                Cursor = Cursors.Hand,
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Tag = fullPath
            };

            Grid grid = new Grid();

            StackPanel stack = new StackPanel { Orientation = Orientation.Horizontal };
            TextBlock icon = new TextBlock
            {
                Text = GetFileIcon(Path.GetExtension(name)),
                FontSize = 16,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock text = new TextBlock
            {
                Text = name,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            stack.Children.Add(icon);
            stack.Children.Add(text);

            TextBlock sizeText = new TextBlock
            {
                Text = size,
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            grid.Children.Add(stack);
            grid.Children.Add(sizeText);
            border.Child = grid;

            border.MouseLeftButtonDown += (s, e) =>
            {
                if (s is Border b && b.Tag is string path)
                {
                    _selectedFilePath = path;
                    txtFileName.Text = Path.GetFileName(path);
                    
                    // Highlight selected item
                    foreach (var child in panelFiles.Children)
                    {
                        if (child is Border childBorder)
                        {
                            childBorder.BorderBrush = System.Windows.Media.Brushes.LightGray;
                            childBorder.BorderThickness = new Thickness(1);
                        }
                    }
                    b.BorderBrush = System.Windows.Media.Brushes.DodgerBlue;
                    b.BorderThickness = new Thickness(2);
                }
            };

            panelFiles.Children.Add(border);
        }

        private string GetFileIcon(string extension)
        {
            return extension.ToLower() switch
            {
                ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" => "🖼️",
                ".mp4" or ".avi" or ".mov" or ".wmv" => "🎥",
                ".pdf" => "📄",
                ".docx" or ".doc" => "📝",
                ".xlsx" or ".xls" => "📊",
                ".pptx" or ".ppt" => "📈",
                ".txt" => "📃",
                ".zip" or ".rar" => "📦",
                _ => "📄"
            };
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.#} {sizes[order]}";
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (_historyIndex > 0)
            {
                _historyIndex--;
                _currentPath = _navigationHistory[_historyIndex];
                txtPath.Text = _currentPath;
                LoadFiles();
            }
        }

        private void btnUp_Click(object sender, RoutedEventArgs e)
        {
            DirectoryInfo parentDir = Directory.GetParent(_currentPath);
            if (parentDir != null)
            {
                NavigateTo(parentDir.FullName);
            }
        }

        private void btnHome_Click(object sender, RoutedEventArgs e)
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            NavigateTo(documentsPath);
        }

        private void btnDesktop_Click(object sender, RoutedEventArgs e)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            NavigateTo(desktopPath);
        }

        private void txtPath_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                NavigateTo(txtPath.Text);
            }
        }

        private void treeViewFolders_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // TODO: Implement tree view navigation
            // This requires building a dynamic tree structure with system folders
        }

        private void cmbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbFilter.SelectedIndex == 0)
                FileFilter = "*.*";
            else if (cmbFilter.SelectedIndex == 1)
                FileFilter = "*.png;*.jpg;*.jpeg;*.gif;*.bmp";
            else if (cmbFilter.SelectedIndex == 2)
                FileFilter = "*.mp4;*.avi;*.mov;*.wmv";
            else if (cmbFilter.SelectedIndex == 3)
                FileFilter = "*.pdf;*.docx;*.doc";

            if (!string.IsNullOrEmpty(_currentPath))
                LoadFiles();
        }

        private void ViewMode_Changed(object sender, RoutedEventArgs e)
        {
            // TODO: Implement grid view layout
            // Currently only list view is implemented
        }

        private void FileItem_Click(object sender, MouseButtonEventArgs e)
        {
            // Already handled in AddFileItem and AddFolderItem methods
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFilePath) && string.IsNullOrEmpty(txtFileName.Text))
            {
                MessageBox.Show("Vui lòng chọn một tệp!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!string.IsNullOrEmpty(txtFileName.Text) && string.IsNullOrEmpty(_selectedFilePath))
            {
                _selectedFilePath = Path.Combine(_currentPath, txtFileName.Text);
            }

            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
