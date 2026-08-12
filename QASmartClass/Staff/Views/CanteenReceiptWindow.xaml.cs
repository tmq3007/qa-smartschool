using System.Windows;

namespace QASmartClass.Staff.Views
{
    public partial class CanteenReceiptWindow : Window
    {
        public CanteenReceiptWindow(string text)
        {
            InitializeComponent();
            ReceiptTxt.Text = text;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Đang gửi lệnh in tới máy in nhiệt tại quầy...", "Thông báo in", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
