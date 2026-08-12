using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SmartLibrary.Desktop.Services;
using SmartLibrary.Desktop.ViewModels;
using SmartLibrary.Desktop.Models;
using SmartLibrary.Desktop.Helpers;

namespace SmartLibrary.Desktop.Views.Librarian
{
    public partial class LostBookDialog : Window
    {
        private readonly string _ssoUserId;
        private decimal _bookPrice = 0;
        private decimal _multiplier1 = 1.5m;
        private decimal _multiplier2 = 2.0m;

        public bool IsConfirmed { get; private set; }
        public decimal PenaltyAmount { get; private set; }
        public string LostBarcode { get; private set; } = string.Empty;
        public string LostBookTitle { get; private set; } = string.Empty;
        public bool IsGracePeriod => GracePeriodCheckBox.IsChecked == true;
        public int LoanId { get; private set; }
        public int CompensationType { get; private set; }

        private readonly ApiService _apiService;

        public LostBookDialog(string ssoUserId, string studentName, ApiService apiService)
        {
            InitializeComponent();
            _ssoUserId = ssoUserId;
            _apiService = apiService;
            ReaderInfoText.Text = $"Độc giả: {studentName} ({ssoUserId})";
            Loaded += async (s, e) =>
            {
                BarcodeTextBox.Focus(); // Bổ sung Auto-focus
                await LoadMultipliersAsync();
            };
        }

        private async Task LoadMultipliersAsync()
        {
            try
            {
                var configs = await _apiService.GetAsync<LibraryConfigDto[]>(ApiEndpoints.LibraryConfig);
                if (configs != null)
                {
                    var m1 = configs.FirstOrDefault(c => c.Key == "LostBookMultiplier1");
                    var m2 = configs.FirstOrDefault(c => c.Key == "LostBookMultiplier2");
                    if (m1 != null && decimal.TryParse(m1.Value, out var val1)) _multiplier1 = val1;
                    if (m2 != null && decimal.TryParse(m2.Value, out var val2)) _multiplier2 = val2;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi lấy cấu hình: {ex.Message}");
                _multiplier1 = 1.5m;
                _multiplier2 = 2.0m;
            }

            try
            {
                if (RuleComboBox != null && RuleComboBox.Items.Count >= 2)
                {
                    if (RuleComboBox.Items[0] is ComboBoxItem item1)
                        item1.Content = $"Hỗ trợ đền bù gấp {_multiplier1:F1} lần giá bìa";
                    if (RuleComboBox.Items[1] is ComboBoxItem item2)
                        item2.Content = $"Hỗ trợ đền bù gấp {_multiplier2:F1} lần giá bìa";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi cập nhật UI: {ex.Message}");
            }
        }

        private async void CheckBook_Click(object sender, RoutedEventArgs e)
        {
            string barcode = BarcodeTextBox.Text.Trim();
            if (string.IsNullOrEmpty(barcode))
            {
                MessageBox.Show("Vui lòng nhập mã vạch (Barcode) sách thất lạc!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Gọi API trạng thái mượn sách chi tiết
                var status = await _apiService.GetAsync<BookStatusResponseDto>($"{ApiEndpoints.CirculationBookStatus}{Uri.EscapeDataString(barcode)}");
                if (status != null)
                {
                    if (status.BorrowerSsoId != _ssoUserId)
                    {
                        MessageBox.Show($"Cuốn sách này đang được mượn bởi độc giả khác ({status.CurrentBorrower})! Vui lòng kiểm tra lại.", "Sai độc giả", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    LoanId = status.LoanId;
                    LostBookTitle = status.BookTitle;
                    _bookPrice = status.Price > 0 ? status.Price : 50000;
                }
                else
                {
                    MessageBox.Show("Không tìm thấy phiếu mượn đang hoạt động của sách thất lạc này!", "Không tìm thấy", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            catch
            {
                // Fallback Offline Mock
                if (barcode.Contains("978") || barcode.Equals("S005"))
                {
                    LostBookTitle = "Đắc Nhân Tâm (Dale Carnegie)";
                    _bookPrice = 80000;
                    LoanId = 1;
                }
                else
                {
                    LostBookTitle = $"Tài liệu tham khảo: {barcode}";
                    _bookPrice = 50000;
                    LoanId = 2;
                }
            }
            
            BookTitleText.Text = LostBookTitle;
            BookPriceText.Text = $"Giá bìa: {_bookPrice:N0} VNĐ";
            RecalculatePenalty();
        }

        private void Rule_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RecalculatePenalty();
        }

        private void RecalculatePenalty()
        {
            if (_bookPrice <= 0 || RuleComboBox == null || FineCalculatedText == null) return;

            int index = RuleComboBox.SelectedIndex;
            if (index == 0) // 1.5x
            {
                CompensationType = 0;
                PenaltyAmount = _bookPrice * _multiplier1;
            }
            else if (index == 1) // 2.0x
            {
                CompensationType = 0;
                PenaltyAmount = _bookPrice * _multiplier2;
            }
            else if (index == 2) // Book replacement
            {
                CompensationType = 1;
                PenaltyAmount = 0;
            }
            else // Community service
            {
                CompensationType = 2;
                PenaltyAmount = 0;
            }

            FineCalculatedText.Text = $"{PenaltyAmount:N0} VNĐ";
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            LostBarcode = BarcodeTextBox.Text.Trim();
            if (string.IsNullOrEmpty(LostBarcode) || _bookPrice <= 0)
            {
                MessageBox.Show("Vui lòng nhập mã vạch (Barcode) và click 'Kiểm tra' để xác thực thông tin sách thất lạc trước!", "Yêu cầu kiểm tra", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsConfirmed = true;
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            this.DialogResult = false;
            this.Close();
        }
    }

    public class BookStatusResponseDto
    {
        public int LoanId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CurrentBorrower { get; set; } = string.Empty;
        public string BorrowerSsoId { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
    }
}
