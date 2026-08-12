using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using QASmartTouch.Forms;
using QASmartClass.Services;

namespace QASmartClass.StudentClient.Views
{
    public partial class StudentGuidePage : Page
    {
        public ObservableCollection<GuideStep> Guides { get; set; }

        public StudentGuidePage()
        {
            InitializeComponent();
            InitializeGuides();
            this.DataContext = this;
        }

        private void InitializeGuides()
        {
            Guides = new ObservableCollection<GuideStep>
            {
                new GuideStep
                {
                    Title = "Bước 1: Kết nối Lớp học",
                    Subtitle = "Đăng nhập và tự động tìm giáo viên",
                    IconCode = "\xE8FA",
                    IconBgColor = "#D1FAE5",
                    IconColor = "#10B981",
                    ContentText = "Hệ thống QA SmartClass được thiết kế để Tự động hóa việc kết nối. Bạn chỉ cần:\n\n1. Mở ứng dụng: Tại cửa sổ Đăng nhập, nhập mã học sinh của bạn (Ví dụ: HS001).\n\n2. Kiểm tra trạng thái mạng: Góc trái màn hình (Menu) sẽ hiển thị trạng thái. Màu Xanh lá (Đã kết nối) báo hiệu bạn đã ở trong lớp. Màu xám nghĩa là bạn cần đợi Giáo viên mở lớp.",
                    IsExpanded = true
                },
                new GuideStep
                {
                    Title = "Bước 2: Học tập & Bài giảng",
                    Subtitle = "Đồng bộ thời gian thực với màn hình Giáo viên",
                    IconCode = "\xE736",
                    IconBgColor = "#DBEAFE",
                    IconColor = "#3B82F6",
                    ContentText = "Lưu ý: Khi Giáo viên bắt đầu giảng bài, màn hình của bạn sẽ tự động chuyển sang tab \"Bài giảng hôm nay\". Bạn không cần thao tác gì thêm!\n\nMẹo nhỏ: Nếu lỡ tay bấm sang tab khác và không thấy màn hình của thầy cô, hãy bấm lại vào nút \"Bài giảng hôm nay\" ở thanh menu bên trái.\n\n• Tài liệu (File Broadcast): Khi GV gửi bài tập (Word, PDF, Video), tài liệu sẽ hiện ở giữa màn hình. Nhấn \"Mở file\" để xem.\n\n• Bảng trắng (Whiteboard): Nội dung nét vẽ của giáo viên sẽ đồng bộ trực tiếp lên màn hình của bạn.",
                    IsExpanded = false
                },
                new GuideStep
                {
                    Title = "Bước 3: Làm bài & Nộp bài",
                    Subtitle = "Trắc nghiệm (Quiz) và Tự luận (File)",
                    IconCode = "\xE8B5",
                    IconBgColor = "#FEF3C7",
                    IconColor = "#D97706",
                    ContentText = "✅ Trắc nghiệm (Quiz)\nMàn hình câu hỏi sẽ hiện lên. Chọn đáp án A, B, C, D và nhấn Gửi Đáp Án. Hãy để ý đồng hồ đếm ngược góc phải.\n\n📝 Bài tập Tự luận\nVào tab Bài tập / Nộp bài. Nhấn nút Tải file lên để gửi ảnh chụp bài giải cho giáo viên chấm trực tiếp.",
                    IsExpanded = false
                },
                new GuideStep
                {
                    Title = "Bước 4: Tương tác trong Lớp",
                    Subtitle = "Giơ tay phát biểu, nhắn tin với giáo viên",
                    IconCode = "\xE774",
                    IconBgColor = "#FCE7F3",
                    IconColor = "#BE185D",
                    ContentText = "• Giơ tay (Hand Raise): Khi bạn có thắc mắc, hãy chuyển qua tab \"Giơ tay / Hỏi GV\" và nhấn nút Giơ tay. Biểu tượng báo hiệu sẽ nhấp nháy trên màn hình chính của giáo viên.\n\n• Nhắn tin (Chat): Bạn có thể gửi tin nhắn riêng cho giáo viên bằng tab \"Tin nhắn\". Lịch sử trò chuyện sẽ được bảo mật.\n\n• Ảnh đại diện: Ở góc dưới cùng bên trái, nhấn vào hình đại diện mặc định để tải lên ảnh của riêng bạn.",
                    IsExpanded = false
                }
            };
        }

        #region Search and Filter Logic

        private void TxtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            lblSearchPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtSearch.Text))
            {
                lblSearchPlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void TxtSearch_KeyUp(object sender, KeyEventArgs e)
        {
            bool hasText = !string.IsNullOrEmpty(txtSearch.Text);
            btnClearSearch.Visibility = hasText ? Visibility.Visible : Visibility.Collapsed;
            lblSearchPlaceholder.Visibility = hasText ? Visibility.Collapsed : Visibility.Visible;
            
            FilterGuide(txtSearch.Text);
        }

        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = string.Empty;
            btnClearSearch.Visibility = Visibility.Collapsed;
            lblSearchPlaceholder.Visibility = Visibility.Visible;
            
            FilterGuide(string.Empty);
            txtSearch.Focus();
        }

        private void FilterGuide(string keyword)
        {
            string cleanKeyword = (keyword ?? "").Trim().ToLower();

            foreach (var step in Guides)
            {
                if (string.IsNullOrEmpty(cleanKeyword))
                {
                    step.IsVisible = true;
                }
                else
                {
                    bool isMatch = (step.Title != null && step.Title.ToLower().Contains(cleanKeyword)) ||
                                   (step.ContentText != null && step.ContentText.ToLower().Contains(cleanKeyword)) ||
                                   (step.Subtitle != null && step.Subtitle.ToLower().Contains(cleanKeyword));
                                   
                    step.IsVisible = isMatch;
                    if (isMatch)
                    {
                        step.IsExpanded = true;
                    }
                }
            }
        }

        #endregion

        #region Help Tour Integration

        private void BtnStartHelpTour_Click(object sender, RoutedEventArgs e)
        {
            var parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                try
                {
                    var tour = new HelpTourOverlay(parentWindow);
                    tour.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể hiển thị Trợ giúp Trực quan: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion
    }

    public class GuideStep : INotifyPropertyChanged
    {
        private bool _isExpanded;
        private bool _isVisible = true;

        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string IconCode { get; set; }
        public string IconBgColor { get; set; }
        public string IconColor { get; set; }
        public string ContentText { get; set; }
        public string ImagePath { get; set; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnPropertyChanged(nameof(IsVisible));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
