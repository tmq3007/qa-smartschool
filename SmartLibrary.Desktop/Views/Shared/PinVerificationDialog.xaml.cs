using System.Windows;
using System.Windows.Controls;

namespace SmartLibrary.Desktop.Views.Shared
{
    public partial class PinVerificationDialog : Window
    {
        private readonly string _correctPinHash;
        public bool IsVerified { get; private set; } = false;

        /// <summary>
        /// correctPinHash: Mã băm SHA-256 của PIN đúng.
        /// </summary>
        public PinVerificationDialog(string correctPinHash)
        {
            InitializeComponent();
            _correctPinHash = correctPinHash;
            
            // Auto-focus vào ô nhập PIN
            Loaded += (s, e) => PinBox.Focus();
            
            // Enter = Xác nhận
            PinBox.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                    BtnConfirm_Click(s, e);
            };
        }

        public int FailedAttempts { get; private set; } = 0;

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (SmartLibrary.Desktop.Helpers.SecurityHelper.ComputeSha256(PinBox.Password) == _correctPinHash)
            {
                IsVerified = true;
                DialogResult = true;
                Close();
            }
            else
            {
                FailedAttempts++;
                if (FailedAttempts >= 3)
                {
                    IsVerified = false;
                    DialogResult = false;
                    Close();
                }
                else
                {
                    ErrorText.Text = $"❌ Mã PIN không đúng! (Còn {3 - FailedAttempts} lần)";
                    PinBox.Password = "";
                    PinBox.Focus();
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            IsVerified = false;
            DialogResult = false;
            Close();
        }

        private void Keypad_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Content is string key)
            {
                ErrorText.Text = ""; // Clear error message when user starts typing again
                if (key == "⌫")
                {
                    if (PinBox.Password.Length > 0)
                        PinBox.Password = PinBox.Password.Substring(0, PinBox.Password.Length - 1);
                }
                else if (key == "C")
                {
                    PinBox.Password = "";
                }
                else if (PinBox.Password.Length < 4)
                {
                    PinBox.Password += key;
                }
            }
        }
    }
}
