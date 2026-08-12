using System;
using System.Windows;
using System.Windows.Input;
using QASmartClass.Services;

namespace QASmartTouch.Forms
{
    public partial class AdminPasswordPromptDialog : Window
    {
        private readonly AppConfig _appConfig;
        private int _failedAttempts = 0;
        private const string SECURE_SALT = "QASmartTouch_SecureSalt_2026_@AdminLock";

        public AdminPasswordPromptDialog()
        {
            InitializeComponent();
            _appConfig = AppConfig.Load();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            pbPassword.Focus();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            ValidatePassword();
        }

        private void pbPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ValidatePassword();
            }
        }

        private void ValidatePassword()
        {
            string inputPassword = pbPassword.Password;
            if (string.IsNullOrEmpty(inputPassword))
            {
                MessageBox.Show("Vui lòng nhập mật khẩu!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Prevent double click / concurrent execution
            btnConfirm.IsEnabled = false;
            pbPassword.IsEnabled = false;

            try
            {
                string inputHash = ConfigurationSecurityHelper.ComputeSha256Hash(inputPassword);
                if (inputHash == _appConfig.AdminPasswordHash)
                {
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    _failedAttempts++;
                    if (_failedAttempts >= 3)
                    {
                        MessageBox.Show("Bạn đã nhập sai mật khẩu quá 3 lần! Hộp thoại sẽ đóng lại.", 
                                        "Khóa bảo mật", MessageBoxButton.OK, MessageBoxImage.Error);
                        this.DialogResult = false;
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show($"Sai mật khẩu! Còn lại {3 - _failedAttempts} lần thử.", 
                                        "Lỗi mật khẩu", MessageBoxButton.OK, MessageBoxImage.Warning);
                        pbPassword.Clear();
                        
                        // Re-enable for retry
                        btnConfirm.IsEnabled = true;
                        pbPassword.IsEnabled = true;
                        pbPassword.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hệ thống: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                btnConfirm.IsEnabled = true;
                pbPassword.IsEnabled = true;
            }
        }
    }
}
