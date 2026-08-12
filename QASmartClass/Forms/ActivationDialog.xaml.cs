using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using QASmartTouch.Services.License;

namespace QASmartTouch.Forms
{
    public partial class ActivationDialog : Window
    {
        public ActivationDialog()
        {
            InitializeComponent();
            Loaded += ActivationDialog_Loaded;
        }

        private void ActivationDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // Hiển thị Request Code
            txtRequestCode.Text = LicenseService.Instance.GetRequestCode();
            
            // Xóa thông báo lỗi ban đầu
            HideFeedback();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(txtRequestCode.Text);
                ShowFeedback("Đã copy mã máy vào clipboard!", true);
            }
            catch (Exception ex)
            {
                ShowFeedback($"Lỗi copy: {ex.Message}", false);
            }
        }

        private void BtnBrowseFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Chọn file bản quyền (.lic)",
                Filter = "License Files (*.lic)|*.lic|All Files (*.*)|*.*",
                CheckFileExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string code = File.ReadAllText(openFileDialog.FileName).Trim();
                    txtActivationCode.Text = code;
                    ShowFeedback("Đã tải mã từ file, vui lòng bấm KÍCH HOẠT.", true);
                }
                catch (Exception ex)
                {
                    ShowFeedback($"Không thể đọc file: {ex.Message}", false);
                }
            }
        }

        private void CmbTrial_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cmbTrial.SelectedItem is System.Windows.Controls.ComboBoxItem item && item.Tag != null)
            {
                string trialPkg = item.Tag.ToString()!;
                ShowFeedback($"Tính năng tự động lấy key {trialPkg} sẽ được cấu hình bởi nhà trường.", true);
                // Ghi chú: Có thể thêm API gọi về server để tự cấp trial key ở đây
            }
        }

        private void BtnActivate_Click(object sender, RoutedEventArgs e)
        {
            string code = txtActivationCode.Text.Trim();
            
            if (string.IsNullOrEmpty(code))
            {
                ShowFeedback("Vui lòng nhập mã kích hoạt hoặc chọn file .lic!", false);
                return;
            }

            // Gọi LicenseService
            var result = LicenseService.Instance.Activate(code);

            if (result.Success)
            {
                ShowFeedback(result.Message, true);
                MessageBox.Show(result.Message, "Kích hoạt thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                ShowFeedback(result.Message, false);
            }
        }

        private void ShowFeedback(string message, bool isSuccess)
        {
            bdFeedback.Visibility = Visibility.Visible;
            txtFeedbackMsg.Text = message;

            if (isSuccess)
            {
                bdFeedback.Background = new SolidColorBrush(Color.FromRgb(240, 253, 244)); // #F0FDF4 (Green 50)
                bdFeedback.BorderBrush = new SolidColorBrush(Color.FromRgb(134, 239, 172)); // #86EFAC (Green 300)
                txtFeedbackIcon.Text = "✅";
                txtFeedbackMsg.Foreground = new SolidColorBrush(Color.FromRgb(21, 128, 61)); // #15803D (Green 700)
            }
            else
            {
                bdFeedback.Background = new SolidColorBrush(Color.FromRgb(254, 242, 242)); // #FEF2F2 (Red 50)
                bdFeedback.BorderBrush = new SolidColorBrush(Color.FromRgb(252, 165, 165)); // #FCA5A5 (Red 300)
                txtFeedbackIcon.Text = "❌";
                txtFeedbackMsg.Foreground = new SolidColorBrush(Color.FromRgb(153, 27, 27)); // #991B1B (Red 700)
            }
        }

        private void HideFeedback()
        {
            bdFeedback.Visibility = Visibility.Collapsed;
        }
    }
}
