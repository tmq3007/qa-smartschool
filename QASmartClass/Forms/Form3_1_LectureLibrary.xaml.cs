using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QASmartTouch.Forms
{
    public partial class Form3_1_LectureLibrary : Window
    {
        private ObservableCollection<LectureItem> _lectures;
        private int _currentPage = 1;
        private int _totalPages = 5;

        public Form3_1_LectureLibrary()
        {
            InitializeComponent();
            LoadSampleData();
        }

        private void LoadSampleData()
        {
            _lectures = new ObservableCollection<LectureItem>
            {
                new LectureItem
                {
                    Name = "Phương trình bậc 2 - Lớp 9",
                    DateCreated = "15/01/2025",
                    Size = "2.4 MB",
                    Tags = "🔴 Toán học, 🔵 Đại số",
                    Status = "✅ Hoàn thành"
                },
                new LectureItem
                {
                    Name = "Định luật Newton - Vật lý 10",
                    DateCreated = "14/01/2025",
                    Size = "3.8 MB",
                    Tags = "🔵 Vật lý, 🟢 Cơ học",
                    Status = "✅ Hoàn thành"
                },
                new LectureItem
                {
                    Name = "Bảng tuần hoàn - Hóa học 8",
                    DateCreated = "13/01/2025",
                    Size = "1.9 MB",
                    Tags = "🟢 Hóa học",
                    Status = "📝 Bản nháp"
                },
                new LectureItem
                {
                    Name = "Phân tích tác phẩm Chí Phèo",
                    DateCreated = "12/01/2025",
                    Size = "1.2 MB",
                    Tags = "🟡 Ngữ văn, 📖 Văn học",
                    Status = "✅ Hoàn thành"
                },
                new LectureItem
                {
                    Name = "Hình học không gian - Lớp 11",
                    DateCreated = "11/01/2025",
                    Size = "4.5 MB",
                    Tags = "🔴 Toán học, 📐 Hình học",
                    Status = "📝 Bản nháp"
                },
                new LectureItem
                {
                    Name = "Điện xoay chiều - Vật lý 12",
                    DateCreated = "10/01/2025",
                    Size = "3.2 MB",
                    Tags = "🔵 Vật lý, ⚡ Điện học",
                    Status = "✅ Hoàn thành"
                },
                new LectureItem
                {
                    Name = "Phản ứng hóa học - Hóa 9",
                    DateCreated = "09/01/2025",
                    Size = "2.1 MB",
                    Tags = "🟢 Hóa học",
                    Status = "✅ Hoàn thành"
                },
                new LectureItem
                {
                    Name = "Lịch sử Việt Nam hiện đại",
                    DateCreated = "08/01/2025",
                    Size = "5.8 MB",
                    Tags = "🟠 Lịch sử, Việt Nam",
                    Status = "📝 Bản nháp"
                }
            };

            dgLectures.ItemsSource = _lectures;
        }

        private void dgLectures_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgLectures.SelectedItem is LectureItem lecture)
            {
                UpdatePreview(lecture);
            }
        }

        private void UpdatePreview(LectureItem lecture)
        {
            txtPreviewName.Text = lecture.Name;
            txtPreviewDate.Text = lecture.DateCreated;
            txtPreviewSize.Text = lecture.Size;
            txtPreviewPages.Text = "24 trang"; // Sample data

            // Update tags
            panelTags.Children.Clear();
            string[] tags = lecture.Tags.Split(',');
            foreach (string tag in tags)
            {
                var border = new Border
                {
                    Background = System.Windows.Media.Brushes.LightBlue,
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(8, 4, 8, 4),
                    Margin = new Thickness(0, 0, 5, 5)
                };
                var text = new TextBlock
                {
                    Text = tag.Trim(),
                    FontSize = 10
                };
                border.Child = text;
                panelTags.Children.Add(border);
            }
        }

        private void btnNewLecture_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Tạo bài giảng mới\n\nChức năng sẽ mở Form2_MainDashboard với canvas trống.", 
                "Tạo mới", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // TODO: Navigate to Form2_MainDashboard with new blank canvas
            DialogResult = true;
            Close();
        }

        private void btnOpenLecture_Click(object sender, RoutedEventArgs e)
        {
            if (dgLectures.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn một bài giảng!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var lecture = dgLectures.SelectedItem as LectureItem;
            MessageBox.Show($"Mở bài giảng: {lecture.Name}\n\nChức năng sẽ load dữ liệu vào Form2_MainDashboard.", 
                "Mở bài giảng", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // TODO: Load lecture data into Form2_MainDashboard
            DialogResult = true;
            Close();
        }

        private void btnDeleteLecture_Click(object sender, RoutedEventArgs e)
        {
            if (dgLectures.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn một bài giảng để xóa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show("Bạn có chắc chắn muốn xóa bài giảng này?\n\nThao tác này không thể hoàn tác!", 
                "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                var lecture = dgLectures.SelectedItem as LectureItem;
                _lectures.Remove(lecture);
                MessageBox.Show("Đã xóa bài giảng thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnDuplicate_Click(object sender, RoutedEventArgs e)
        {
            if (dgLectures.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn một bài giảng để sao chép!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var lecture = dgLectures.SelectedItem as LectureItem;
            var copy = new LectureItem
            {
                Name = lecture.Name + " (Bản sao)",
                DateCreated = DateTime.Now.ToString("dd/MM/yyyy"),
                Size = lecture.Size,
                Tags = lecture.Tags,
                Status = "📝 Bản nháp"
            };
            _lectures.Insert(0, copy);
            MessageBox.Show("Đã sao chép bài giảng thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            if (dgLectures.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn một bài giảng để xuất!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new Window
            {
                Title = "Xuất file",
                Width = 400,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var stack = new StackPanel { Margin = new Thickness(20) };
            stack.Children.Add(new TextBlock { Text = "Chọn định dạng xuất:", FontSize = 14, Margin = new Thickness(0, 0, 0, 10) });
            
            var btnPDF = new Button { Content = "📄 PDF (Portable Document Format)", Height = 40, Margin = new Thickness(0, 0, 0, 10) };
            btnPDF.Click += (s, ev) => { MessageBox.Show("Xuất PDF thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information); dialog.Close(); };
            
            var btnPPTX = new Button { Content = "📊 PowerPoint (PPTX)", Height = 40, Margin = new Thickness(0, 0, 0, 10) };
            btnPPTX.Click += (s, ev) => { MessageBox.Show("Xuất PowerPoint thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information); dialog.Close(); };
            
            var btnImages = new Button { Content = "🖼️ Hình ảnh (PNG)", Height = 40 };
            btnImages.Click += (s, ev) => { MessageBox.Show("Xuất hình ảnh thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information); dialog.Close(); };

            stack.Children.Add(btnPDF);
            stack.Children.Add(btnPPTX);
            stack.Children.Add(btnImages);
            dialog.Content = stack;
            dialog.ShowDialog();
        }

        private void btnCloudSync_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Đang đồng bộ với Cloud...\n\n✅ Đã đồng bộ 8 bài giảng\n⏳ Thời gian: 2 giây\n📊 Dung lượng: 24.8 MB", 
                "Đồng bộ Cloud", MessageBoxButton.OK, MessageBoxImage.Information);
            txtSyncStatus.Text = "✅ Đã đồng bộ (vừa xong)";
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            string query = txtSearch.Text.ToLower();
            if (string.IsNullOrWhiteSpace(query) || query == "tìm kiếm bài giảng...")
            {
                LoadSampleData();
                return;
            }

            var filtered = _lectures.Where(l => l.Name.ToLower().Contains(query)).ToList();
            dgLectures.ItemsSource = new ObservableCollection<LectureItem>(filtered);
            MessageBox.Show($"Tìm thấy {filtered.Count} kết quả!", "Kết quả tìm kiếm", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void cmbSortBy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_lectures == null) return;

            if (cmbSortBy.SelectedIndex == 0) // Newest
                dgLectures.ItemsSource = new ObservableCollection<LectureItem>(_lectures.OrderByDescending(l => l.DateCreated));
            else if (cmbSortBy.SelectedIndex == 1) // Oldest
                dgLectures.ItemsSource = new ObservableCollection<LectureItem>(_lectures.OrderBy(l => l.DateCreated));
            else if (cmbSortBy.SelectedIndex == 2) // Name A-Z
                dgLectures.ItemsSource = new ObservableCollection<LectureItem>(_lectures.OrderBy(l => l.Name));
            else if (cmbSortBy.SelectedIndex == 3) // Size
                dgLectures.ItemsSource = new ObservableCollection<LectureItem>(_lectures.OrderByDescending(l => l.Size));
        }

        private void btnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                MessageBox.Show($"Chuyển đến trang {_currentPage}", "Phân trang", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                MessageBox.Show($"Chuyển đến trang {_currentPage}", "Phân trang", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnQuickOpen_Click(object sender, RoutedEventArgs e)
        {
            btnOpenLecture_Click(sender, e);
        }

        private void btnQuickRename_Click(object sender, RoutedEventArgs e)
        {
            if (dgLectures.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn một bài giảng!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var lecture = dgLectures.SelectedItem as LectureItem;
            var dialog = new Window
            {
                Title = "Đổi tên bài giảng",
                Width = 400,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var stack = new StackPanel { Margin = new Thickness(20) };
            stack.Children.Add(new TextBlock { Text = "Tên mới:", FontSize = 12, Margin = new Thickness(0, 0, 0, 5) });
            
            var txtNewName = new TextBox { Text = lecture.Name, Height = 35, Margin = new Thickness(0, 0, 0, 15) };
            stack.Children.Add(txtNewName);

            var btnOK = new Button { Content = "Đổi tên", Height = 35, Background = System.Windows.Media.Brushes.DodgerBlue, Foreground = System.Windows.Media.Brushes.White };
            btnOK.Click += (s, ev) =>
            {
                lecture.Name = txtNewName.Text;
                dgLectures.Items.Refresh();
                UpdatePreview(lecture);
                MessageBox.Show("Đã đổi tên thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                dialog.Close();
            };
            stack.Children.Add(btnOK);

            dialog.Content = stack;
            dialog.ShowDialog();
        }

        private void btnQuickExport_Click(object sender, RoutedEventArgs e)
        {
            btnExport_Click(sender, e);
        }

        private void btnQuickShare_Click(object sender, RoutedEventArgs e)
        {
            if (dgLectures.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn một bài giảng để chia sẻ!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBox.Show("Chọn phương thức chia sẻ:\n\n📱 QR Code\n☁️ Cloud Link\n📧 Email\n💾 USB Export", 
                "Chia sẻ bài giảng", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    // Data model for lectures
    public class LectureItem
    {
        public string Name { get; set; }
        public string DateCreated { get; set; }
        public string Size { get; set; }
        public string Tags { get; set; }
        public string Status { get; set; }
    }
}
