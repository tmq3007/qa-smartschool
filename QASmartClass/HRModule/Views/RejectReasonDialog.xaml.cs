using System;
using System.Windows;

namespace QASmartClass.HRModule.Views
{
    public partial class RejectReasonDialog : Window
    {
        public string ReasonText { get; private set; } = "Không được chấp thuận";

        public RejectReasonDialog()
        {
            InitializeComponent();
            Loaded += (s, e) => { TxtReason.Focus(); TxtReason.SelectAll(); };
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            ReasonText = TxtReason.Text.Trim();
            if (string.IsNullOrEmpty(ReasonText))
            {
                MessageBox.Show("Vui lòng nhập lý do từ chối.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
            Close();
        }
    }
}
