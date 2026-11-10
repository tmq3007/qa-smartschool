using QASmartTouch.Services;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;
using QASmartTouch.Helpers;

namespace QASmartTouch.Forms
{
    public partial class Form2_OCRResultDialog : Window
    {
        public string? SelectedText { get; private set; }
        private List<OCRResult> _results;

        public Form2_OCRResultDialog(BitmapSource previewImage, List<OCRResult> results)
        {
            InitializeComponent();
            TouchActivationHelper.ApplyToWindow(this);
            
            _results = results;
            
            // Set preview image
            PreviewImage.Source = previewImage;
            
            // Populate ComboBox
            ResultsComboBox.ItemsSource = results;
            
            // Auto-select first result if available
            if (results.Count > 0)
            {
                ResultsComboBox.SelectedIndex = 0;
            }
        }

        private void ResultsComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ResultsComboBox.SelectedItem is OCRResult selected)
            {
                SelectedTextPreview.Text = $"📝 Đã chọn: \"{selected.Text}\" (Độ tin cậy: {selected.Confidence:P0})";
                btnInsert.IsEnabled = true;
            }
            else
            {
                SelectedTextPreview.Text = "Chưa chọn kết quả";
                btnInsert.IsEnabled = false;
            }
        }

        private void btnInsert_Click(object sender, RoutedEventArgs e)
        {
            if (ResultsComboBox.SelectedItem is OCRResult selected)
            {
                SelectedText = selected.Text;
                DialogResult = true;
                Close();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
