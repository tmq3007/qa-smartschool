using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QASmartTouch.Models;
using QASmartTouch.Helpers;

namespace QASmartTouch.Forms
{
    /// <summary>
    /// Dialog hiển thị kết quả nhận dạng chữ viết
    /// </summary>
    public partial class HandwritingRecognitionDialog : Window
    {
        #region Fields

        private List<HandwritingRecognitionResult> _results = new();
        private HandwritingRecognitionResult? _selectedResult;

        #endregion

        #region Properties

        /// <summary>
        /// Kết quả được chọn bởi user
        /// </summary>
        public HandwritingRecognitionResult? SelectedResult => _selectedResult;

        /// <summary>
        /// User có muốn thay thế nét vẽ bằng văn bản không
        /// </summary>
        public bool ReplaceStrokes => chkReplaceStrokes.IsChecked == true;

        /// <summary>
        /// Trạng thái xác nhận của người dùng (dùng thay DialogResult khi Window mở bằng Show())
        /// </summary>
        public bool UserAccepted { get; private set; } = false;

        #endregion

        #region Constructor

        public HandwritingRecognitionDialog()
        {
            InitializeComponent();
            TouchActivationHelper.ApplyToWindow(this);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Hiển thị trạng thái loading
        /// </summary>
        public void ShowLoading()
        {
            pnlLoading.Visibility = Visibility.Visible;
            pnlResults.Visibility = Visibility.Collapsed;
            pnlError.Visibility = Visibility.Collapsed;
            btnInsert.IsEnabled = false;
        }

        /// <summary>
        /// Hiển thị kết quả nhận dạng
        /// </summary>
        public void ShowResults(List<HandwritingRecognitionResult> results)
        {
            _results = results ?? new List<HandwritingRecognitionResult>();

            pnlLoading.Visibility = Visibility.Collapsed;
            pnlError.Visibility = Visibility.Collapsed;

            if (_results.Count == 0)
            {
                // No results found
                pnlNoResults.Visibility = Visibility.Visible;
                pnlResultsList.Visibility = Visibility.Collapsed;
                btnInsert.IsEnabled = false;
                _selectedResult = null;
                if (txtCustomText != null) txtCustomText.Text = "";
                return;
            }

            // Mặc định chọn ngay phương án đầu tiên (Rank 0)
            _selectedResult = _results[0];
            if (txtCustomText != null) txtCustomText.Text = _results[0].Text;

            // Show results
            pnlNoResults.Visibility = Visibility.Collapsed;
            pnlResultsList.Visibility = Visibility.Visible;
            pnlResults.Visibility = Visibility.Visible;

            // Clear previous results
            pnlResultsList.Children.Clear();

            // Add radio buttons for each result
            bool isFirst = true;
            foreach (var result in _results)
            {
                var radioButton = CreateResultRadioButton(result, isFirst);
                pnlResultsList.Children.Add(radioButton);
                isFirst = false;
            }

            // Enable insert button if we have results
            btnInsert.IsEnabled = _results.Count > 0;
        }

        /// <summary>
        /// Hiển thị lỗi
        /// </summary>
        public void ShowError(string errorMessage)
        {
            pnlLoading.Visibility = Visibility.Collapsed;
            pnlResults.Visibility = Visibility.Collapsed;
            pnlError.Visibility = Visibility.Visible;
            txtErrorMessage.Text = errorMessage;
            btnInsert.IsEnabled = false;
            _selectedResult = null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Tạo RadioButton cho mỗi kết quả
        /// </summary>
        private RadioButton CreateResultRadioButton(HandwritingRecognitionResult result, bool isChecked)
        {
            var radioButton = new RadioButton
            {
                Style = (Style)FindResource("ResultRadioButtonStyle"),
                Content = result.Text,
                Tag = result,
                GroupName = "RecognitionResults"
            };

            // Đăng ký event handler Checked TRƯỚC KHI thiết lập IsChecked
            radioButton.Checked += (s, e) =>
            {
                _selectedResult = result;
                if (txtCustomText != null && txtCustomText.Text != result.Text)
                {
                    txtCustomText.Text = result.Text;
                }
                btnInsert.IsEnabled = true;
            };

            // Thiết lập IsChecked sau khi đã đăng ký event handler
            radioButton.IsChecked = isChecked;

            // Set confidence text in the template
            radioButton.Loaded += (s, e) =>
            {
                if (radioButton.Template.FindName("ConfidenceText", radioButton) is TextBlock confidenceText)
                {
                    confidenceText.Text = $"{result.ConfidencePercent}% tin cậy";
                }
            };

            return radioButton;
        }

        private void txtCustomText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtCustomText == null) return;
            string text = txtCustomText.Text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                _selectedResult = new HandwritingRecognitionResult
                {
                    Text = text,
                    Confidence = 1.0f,
                    Rank = 0
                };
                btnInsert.IsEnabled = true;
            }
        }

        /// <summary>
        /// Gán DialogResult an toàn — chỉ hoạt động khi cửa sổ được mở bằng ShowDialog().
        /// Nếu cửa sổ được mở bằng Show(), bỏ qua và gọi Close() thay thế.
        /// </summary>
        private void SafeSetDialogResult(bool value)
        {
            try
            {
                DialogResult = value;
            }
            catch (InvalidOperationException)
            {
                // Window was opened via Show() (non-modal), DialogResult is not supported.
                // UserAccepted property is used instead.
            }
        }

        #endregion

        #region Event Handlers

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            UserAccepted = false;
            SafeSetDialogResult(false);
            Close();
        }

        private void btnInsert_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedResult == null && _results != null && _results.Count > 0)
            {
                _selectedResult = _results[0];
            }

            if (_selectedResult == null)
            {
                MessageBox.Show(
                    "Vui lòng chọn một phương án.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            UserAccepted = true;
            SafeSetDialogResult(true);
            Close();
        }

        #endregion
    }
}
