using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QASmartClass.Services.Tools;

namespace QASmartTouch.Forms
{
    /// <summary>
    /// Hộp thoại nhập biểu thức hàm số và xuất Bảng biến thiên lên bảng vẽ
    /// Hoạt động 100% độc lập, không dùng GeoGebra hay internet
    /// </summary>
    public partial class Form2_VariationTableDialog : Window
    {
        private readonly Form2_MainDashboard? _mainDashboard;
        private VariationModel? _currentModel;

        public Form2_VariationTableDialog()
        {
            InitializeComponent();
            Loaded += Form2_VariationTableDialog_Loaded;
        }

        public Form2_VariationTableDialog(Form2_MainDashboard? mainDashboard) : this()
        {
            _mainDashboard = mainDashboard;
        }

        public void SetExpression(string expr)
        {
            if (!string.IsNullOrWhiteSpace(expr))
            {
                txtExpression.Text = expr;
                txtExpression.CaretIndex = txtExpression.Text.Length;
            }
        }

        private void Form2_VariationTableDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // Tự động phân tích biểu thức mặc định ban đầu
            AnalyzeCurrentExpression();
            txtExpression.Focus();
            txtExpression.SelectAll();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                try { DragMove(); } catch { }
            }
        }

        private void BtnKeyboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Sử dụng chính xác Bàn phím ảo Windows (OSK) sẵn có của phần mềm
                string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string oskPath = System.IO.Path.Combine(windir, "sysnative", "osk.exe");
                
                if (!System.IO.File.Exists(oskPath))
                {
                    oskPath = System.IO.Path.Combine(windir, "System32", "osk.exe");
                }
                
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = oskPath,
                    UseShellExecute = true
                });

                txtExpression.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi mở bàn phím ảo:\n{ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TxtExpression_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AnalyzeCurrentExpression();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        private void TxtExpression_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (lblErrorMessage != null) lblErrorMessage.Visibility = Visibility.Collapsed;
        }

        private void BtnPreview_Click(object sender, RoutedEventArgs e)
        {
            AnalyzeCurrentExpression();
        }

        private void BtnKey_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string key)
            {
                int caret = txtExpression.CaretIndex;
                txtExpression.Text = txtExpression.Text.Insert(caret, key);
                txtExpression.CaretIndex = caret + key.Length;
                txtExpression.Focus();
            }
        }

        private void BtnSample_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string sample)
            {
                txtExpression.Text = sample;
                txtExpression.CaretIndex = txtExpression.Text.Length;
                AnalyzeCurrentExpression();
            }
        }

        private void AnalyzeCurrentExpression()
        {
            try
            {
                lblErrorMessage.Visibility = Visibility.Collapsed;
                string expr = txtExpression.Text.Trim();
                if (string.IsNullOrWhiteSpace(expr))
                {
                    lblErrorMessage.Text = "Vui lòng nhập hàm số f(x).";
                    lblErrorMessage.Visibility = Visibility.Visible;
                    return;
                }

                _currentModel = VariationAnalysisEngine.Analyze(expr);

                // Dựng card preview
                var card = VariationCardRenderer.CreateVariationCard(_currentModel);
                pnlPreviewContainer.Children.Clear();
                pnlPreviewContainer.Children.Add(card);

                btnInsertToBoard.IsEnabled = true;
            }
            catch (Exception ex)
            {
                _currentModel = null;
                pnlPreviewContainer.Children.Clear();
                lblErrorMessage.Text = $"Lỗi phân tích: {ex.Message}";
                lblErrorMessage.Visibility = Visibility.Visible;
                btnInsertToBoard.IsEnabled = false;
            }
        }

        private void BtnInsertToBoard_Click(object sender, RoutedEventArgs e)
        {
            if (_currentModel == null) return;

            try
            {
                if (_mainDashboard != null)
                {
                    _mainDashboard.InsertVariationTableToBoard(_currentModel);
                }
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể đưa lên bảng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
