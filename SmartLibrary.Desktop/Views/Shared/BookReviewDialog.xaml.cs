using System.Windows;
using System.Windows.Controls;

namespace SmartLibrary.Desktop.Views.Shared
{
    public partial class BookReviewDialog : Window
    {
        public int RatingStar { get; private set; } = 5; // Mặc định 5 sao
        public string ReviewContent { get; private set; } = string.Empty;

        public BookReviewDialog(string bookTitle)
        {
            InitializeComponent();
            TxtBookTitle.Text = bookTitle;
            UpdateStars();
            TxtContent.Focus();
        }

        private void Star_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int stars))
            {
                RatingStar = stars;
                UpdateStars();
            }
        }

        private void UpdateStars()
        {
            Button[] starButtons = { BtnStar1, BtnStar2, BtnStar3, BtnStar4, BtnStar5 };
            for (int i = 0; i < starButtons.Length; i++)
            {
                if (i < RatingStar)
                {
                    starButtons[i].Content = "★";
                    starButtons[i].Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F59E0B"));
                }
                else
                {
                    starButtons[i].Content = "☆";
                    starButtons[i].Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#94A3B8"));
                }
            }
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            var content = TxtContent.Text.Trim();
            if (string.IsNullOrWhiteSpace(content) || content.Length < 10)
            {
                TxtError.Text = "❌ Vui lòng nhập nội dung đánh giá tối thiểu 10 ký tự.";
                return;
            }

            ReviewContent = content;
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                this.DragMove();
            }
        }
    }
}
