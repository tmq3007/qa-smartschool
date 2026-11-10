using System;
using System.Windows;
using System.Windows.Navigation;

namespace QASmartTouch.Forms
{
    public partial class Form2_24_AIAssistantWindow : Window
    {
        public Form2_24_AIAssistantWindow()
        {
            InitializeComponent();
        }

        private void Frame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            MessageBox.Show($"Lỗi tải trang Trợ lý AI: {e.Exception.Message}", "Lỗi tải trang", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
