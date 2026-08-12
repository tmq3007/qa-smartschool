using System;
using System.Text.RegularExpressions;
using System.Windows;

namespace QASmartClass.TeacherHub.Views
{
    public partial class RejectCommentWindow : Window
    {
        public string Comment { get; private set; } = string.Empty;

        public RejectCommentWindow()
        {
            InitializeComponent();
            TxtComment.Focus();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            string rawComment = TxtComment.Text.Trim();
            if (string.IsNullOrWhiteSpace(rawComment))
            {
                MessageBox.Show("Vui lòng nhập nội dung góp ý hoặc yêu cầu hiệu chỉnh.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string sanitized = SanitizeInput(rawComment);
            Comment = sanitized;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TxtComment_GotFocus(object sender, RoutedEventArgs e)
        {
            TxtComment.SelectAll();
        }

        private string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            
            // Chỉ loại bỏ các ký tự ASCII control không in được để tránh lỗi hiển thị XML/JSON
            string cleaned = Regex.Replace(input, @"[\x00-\x08\x0B\x0C\x0E-\x1F]", "");

            return cleaned.Trim();
        }
    }
}
