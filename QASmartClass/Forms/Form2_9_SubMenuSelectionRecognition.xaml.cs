using System;
using System.Windows;
using QASmartTouch.Shared;

namespace QASmartTouch.Forms
{
    public partial class Form2_9_SubMenuSelectionRecognition : Window
    {
        public string? SelectedFunction { get; private set; }
        
        public Form2_9_SubMenuSelectionRecognition()
        {
            InitializeComponent();
            if (btnClose != null)
            {
                System.Windows.Input.Stylus.SetIsPressAndHoldEnabled(btnClose, false);

                btnClose.PreviewTouchDown += (s, e) =>
                {
                    e.TouchDevice.Capture(btnClose);
                    e.Handled = true;
                };

                btnClose.PreviewTouchUp += (s, e) =>
                {
                    if (e.TouchDevice.Captured == btnClose)
                    {
                        btnClose.ReleaseTouchCapture(e.TouchDevice);
                    }
                    e.Handled = true;
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try 
                        {
                            this.Owner?.Activate();
                            this.Close();
                        } 
                        catch { }
                    }), System.Windows.Threading.DispatcherPriority.Normal);
                };

                btnClose.PreviewStylusDown += (s, e) =>
                {
                    e.StylusDevice.Capture(btnClose);
                    e.Handled = true;
                };

                btnClose.PreviewStylusUp += (s, e) =>
                {
                    if (e.StylusDevice.Captured == btnClose)
                    {
                        btnClose.ReleaseStylusCapture();
                    }
                    e.Handled = true;
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try 
                        {
                            this.Owner?.Activate();
                            this.Close();
                        } 
                        catch { }
                    }), System.Windows.Threading.DispatcherPriority.Normal);
                };
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            try { this.Owner?.Activate(); } catch { }
            this.Close();
        }

        private void btnOCR_Click(object sender, RoutedEventArgs e)
        {
            SelectedFunction = "OCR";
            MessageBox.Show("Chức năng OCR - Nhận dạng chữ viết tay\n\nSẽ được tích hợp trong phiên bản tiếp theo", 
                "OCR", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnTranslate_Click(object sender, RoutedEventArgs e)
        {
            SelectedFunction = "Translate";
            MessageBox.Show("Chức năng dịch văn bản\n\nSẽ được tích hợp trong phiên bản tiếp theo", 
                "Dịch", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnTTS_Click(object sender, RoutedEventArgs e)
        {
            SelectedFunction = "TTS";
            MessageBox.Show("Chức năng phát âm văn bản\n\nSẽ được tích hợp trong phiên bản tiếp theo", 
                "Phát âm", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnSearchGoogle_Click(object sender, RoutedEventArgs e)
        {
            SelectedFunction = "SearchGoogle";
            MessageBox.Show("Chức năng tìm kiếm Google\n\nSẽ được tích hợp trong phiên bản tiếp theo", 
                "Tìm kiếm", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public string SelectedText { get; set; } = string.Empty;

        private void btnAIChat_Click(object sender, RoutedEventArgs e)
        {
            SelectedFunction = "AIChat";
            var askAI = new AskAIWindow(SelectedText);
            this.Close(); // Đóng submenu TRƯỚC khi mở dialog
            WindowHelper.ShowChildDialog(askAI);
        }
    }
}
