using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

        public Form2_VariationTableDialog(Form2_MainDashboard? mainDashboard, bool defaultToGraph = false) : this()
        {
            _mainDashboard = mainDashboard;
            _isGraphPreviewMode = defaultToGraph;
        }

        private bool _isGraphPreviewMode = false;
        private bool _hasCustomExpression = false;

        public void SetExpression(string expr)
        {
            if (!string.IsNullOrWhiteSpace(expr))
            {
                _hasCustomExpression = true;
                txtExpression.Text = expr;
                txtExpression.CaretIndex = txtExpression.Text.Length;
                AnalyzeCurrentExpression();
            }
        }

        private void Form2_VariationTableDialog_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateTabStyles();
            // Nếu chưa có biểu thức từ bên ngoài nạp vào, mới chạy biểu thức mặc định
            if (!_hasCustomExpression)
            {
                AnalyzeCurrentExpression();
            }
            txtExpression.Focus();
            txtExpression.SelectAll();
        }

        private void UpdateTabStyles()
        {
            if (btnTabBBT == null || btnTabGraph == null) return;
            if (_isGraphPreviewMode)
            {
                btnTabGraph.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235)); // #2563EB
                btnTabGraph.Foreground = Brushes.White;
                btnTabBBT.Background = new SolidColorBrush(Color.FromRgb(226, 232, 240)); // #E2E8F0
                btnTabBBT.Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)); // #475569

                if (btnInsertToBoard != null)
                {
                    btnInsertToBoard.Content = "↗ Đưa Đồ thị lên bảng";
                    btnInsertToBoard.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235)); // #2563EB
                }
            }
            else
            {
                btnTabBBT.Background = new SolidColorBrush(Color.FromRgb(22, 139, 130)); // #168B82
                btnTabBBT.Foreground = Brushes.White;
                btnTabGraph.Background = new SolidColorBrush(Color.FromRgb(226, 232, 240)); // #E2E8F0
                btnTabGraph.Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)); // #475569

                if (btnInsertToBoard != null)
                {
                    btnInsertToBoard.Content = "↗ Đưa Bảng biến thiên lên bảng";
                    btnInsertToBoard.Background = new SolidColorBrush(Color.FromRgb(22, 139, 130)); // #168B82
                }
            }
        }

        private void BtnTabBBT_Click(object sender, RoutedEventArgs e)
        {
            _isGraphPreviewMode = false;
            UpdateTabStyles();
            RenderPreview();
        }

        private void BtnTabGraph_Click(object sender, RoutedEventArgs e)
        {
            _isGraphPreviewMode = true;
            UpdateTabStyles();
            RenderPreview();
        }

        private void RenderPreview()
        {
            if (pnlPreviewContainer == null) return;
            pnlPreviewContainer.Children.Clear();
            if (_currentModel == null) return;

            string expr = txtExpression.Text.Trim();
            if (_isGraphPreviewMode)
            {
                var card = FunctionGraphCardRenderer.CreateGraphCard(expr, _currentModel);
                pnlPreviewContainer.Children.Add(card);
            }
            else
            {
                var card = VariationCardRenderer.CreateVariationCard(_currentModel);
                pnlPreviewContainer.Children.Add(card);
            }
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

                // Dựng card preview theo tab hiện tại (BBT hoặc Đồ thị)
                RenderPreview();

                btnInsertToBoard.IsEnabled = true;
                btnInsertGraphToBoard.IsEnabled = true;
            }
            catch (Exception ex)
            {
                _currentModel = null;
                pnlPreviewContainer.Children.Clear();
                lblErrorMessage.Text = $"Lỗi phân tích: {ex.Message}";
                lblErrorMessage.Visibility = Visibility.Visible;
                btnInsertToBoard.IsEnabled = false;
                btnInsertGraphToBoard.IsEnabled = false;
            }
        }

        private void BtnInsertGraphToBoard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string expr = txtExpression.Text.Trim();
                if (string.IsNullOrWhiteSpace(expr)) return;

                if (_mainDashboard != null)
                {
                    _mainDashboard.InsertFunctionGraphToBoard(expr, _currentModel);
                }
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể đưa đồ thị lên bảng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnInsertToBoard_Click(object sender, RoutedEventArgs e)
        {
            if (_currentModel == null) return;

            try
            {
                if (_mainDashboard != null)
                {
                    if (_isGraphPreviewMode)
                    {
                        string expr = txtExpression.Text.Trim();
                        if (!string.IsNullOrWhiteSpace(expr))
                        {
                            _mainDashboard.InsertFunctionGraphToBoard(expr, _currentModel);
                        }
                    }
                    else
                    {
                        _mainDashboard.InsertVariationTableToBoard(_currentModel);
                    }
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
