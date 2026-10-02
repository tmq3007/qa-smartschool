using System;
using System.Windows;

namespace QASmartTouch.Forms
{
    public partial class ParentLoginHostWindow : Window
    {
        public ParentLoginHostWindow()
        {
            InitializeComponent();
            MessageBox.Show("Chức năng Phụ Huynh đã bị loại bỏ.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Loaded += (s, e) => { this.DialogResult = false; this.Close(); };
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
