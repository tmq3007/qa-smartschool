using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace QASmartClass.Admin.Views
{
    public partial class PinDialog : Window
    {
        private readonly Services.AdminSecurityService _security;
        private DispatcherTimer _lockoutTimer;

        public PinDialog()
        {
            InitializeComponent();
            _security = new Services.AdminSecurityService();
            
            if (_security.IsFirstSetup)
            {
                // UI changes for first setup
                lblTitle.Text = "🔒 TẠO MÃ PIN MỚI";
            }
            else if (_security.IsLockedOut)
            {
                StartLockoutTimer();
            }

            txtPin.Focus();
        }

        private void StartLockoutTimer()
        {
            txtPin.IsEnabled = false;
            btnConfirm.IsEnabled = false;
            
            txtError.Visibility = Visibility.Visible;
            txtError.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Orange/Yellow

            _lockoutTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _lockoutTimer.Tick += (s, e) =>
            {
                if (!_security.IsLockedOut)
                {
                    _lockoutTimer.Stop();
                    txtPin.IsEnabled = true;
                    btnConfirm.IsEnabled = true;
                    txtError.Visibility = Visibility.Collapsed;
                    txtPin.Focus();
                }
                else
                {
                    var rem = _security.LockoutTimeRemaining;
                    txtError.Text = $"Đã khóa! Thử lại sau {rem.Minutes:D2}:{rem.Seconds:D2}";
                }
            };
            _lockoutTimer.Start();
            
            var initialRem = _security.LockoutTimeRemaining;
            txtError.Text = $"Đã khóa! Thử lại sau {initialRem.Minutes:D2}:{initialRem.Seconds:D2}";
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            ValidatePin();
        }

        private void txtPin_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ValidatePin();
            }
            else
            {
                if (!_security.IsLockedOut)
                    txtError.Visibility = Visibility.Collapsed;
            }
        }

        private void ValidatePin()
        {
            if (_security.IsLockedOut) return;

            string pin = txtPin.Password;

            if (string.IsNullOrWhiteSpace(pin))
            {
                txtError.Text = "Vui lòng nhập mã PIN!";
                txtError.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
                txtError.Visibility = Visibility.Visible;
                return;
            }

            if (_security.IsFirstSetup)
            {
                try
                {
                    _security.ChangePin(pin);
                    MessageBox.Show("Đã tạo mã PIN thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
                catch (Exception ex)
                {
                    txtError.Text = "Lỗi: " + ex.Message;
                    txtError.Visibility = Visibility.Visible;
                }
            }
            else
            {
                if (_security.ValidatePin(pin))
                {
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    if (_security.IsLockedOut)
                    {
                        StartLockoutTimer();
                    }
                    else
                    {
                        txtError.Text = $"Mã PIN không đúng! Còn {_security.RemainingAttempts} lần thử.";
                        txtError.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
                        txtError.Visibility = Visibility.Visible;
                    }
                    txtPin.Password = "";
                    txtPin.Focus();
                }
            }
        }
    }
}
