using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SmartLibrary.Desktop.Services;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class AssetDepreciationViewModel : ObservableObject, IActiveAwareViewModel
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private int _selectedYear = DateTime.Now.Year;

        [ObservableProperty]
        private bool _isBusy;

        public bool IsNotBusy => !IsBusy;

        partial void OnIsBusyChanged(bool value)
        {
            OnPropertyChanged(nameof(IsNotBusy));
        }

        [ObservableProperty]
        private string _statusMessage = "";

        [ObservableProperty]
        private decimal _totalOriginalValue;

        [ObservableProperty]
        private decimal _totalRemainingValue;

        public ObservableCollection<int> AvailableYears { get; } = new();
        public ObservableCollection<DepreciationResultDto> DepreciationResults { get; } = new();

        public ICommand CalculateCommand { get; }
        public ICommand ExportExcelCommand { get; }

        public AssetDepreciationViewModel(ApiService apiService)
        {
            _apiService = apiService;
            CalculateCommand = new AsyncRelayCommand(CalculateDepreciationAsync);
            ExportExcelCommand = new AsyncRelayCommand(ExportExcelAsync);

            // Populate years from 2020 (school foundation) to 5 years in the future
            int currentYear = DateTime.Now.Year;
            for (int y = 2020; y <= currentYear + 5; y++)
            {
                AvailableYears.Add(y);
            }
            SelectedYear = currentYear;

        }

        public void Activate()
        {
            _ = CalculateDepreciationAsync();
        }

        public async Task CalculateDepreciationAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = $"Đang tính hao mòn sách in năm {SelectedYear}...";

            try
            {
                // Gọi API lấy toàn bộ danh mục sách
                var books = await _apiService.GetAsync<List<BookDepreciationSourceDto>>("/Books");
                if (books != null)
                {
                    FilterAndCalculate(books);
                }
                else
                {
                    LoadMockData();
                }
            }
            catch (Exception ex)
            {
                await AuditLogService.WriteLogAsync("Hao mòn tài sản", $"Lỗi kết nối API khi tính hao mòn sách in năm {SelectedYear}: {ex.Message}. Đang nạp dữ liệu offline.", false);
                LoadMockData();
            }

            IsBusy = false;
            StatusMessage = "";
        }

        private void FilterAndCalculate(List<BookDepreciationSourceDto> sourceBooks)
        {
            DepreciationResults.Clear();
            decimal totalOriginal = 0;
            decimal totalRemaining = 0;

            // Chỉ tính hao mòn cho sách in (IsDigital == false) và đã mua trước hoặc trong năm tính khấu hao
            var printedBooks = sourceBooks
                .Where(b => !b.IsDigital && (b.PurchaseDate != default ? b.PurchaseDate.Year : DateTime.Now.Year - 3) <= SelectedYear)
                .ToList();

            foreach (var b in printedBooks)
            {
                // Giá trị mặc định nếu dữ liệu trống
                decimal originalPrice = b.Price > 0 ? b.Price : 120000;
                DateTime purchaseDate = b.PurchaseDate != default ? b.PurchaseDate : DateTime.Now.AddYears(-3);
                int usefulLife = b.UsefulLifeYears > 0 ? b.UsefulLifeYears : 5;

                int purchaseYear = purchaseDate.Year;
                int age = SelectedYear - purchaseYear;
                if (age < 0) age = 0;

                double annualRate = 100.0 / usefulLife; // 20% mỗi năm
                double accumulatedRate = Math.Min(100.0, age * annualRate);
                decimal accumulatedValue = originalPrice * (decimal)(accumulatedRate / 100.0);
                decimal remainingValue = originalPrice - accumulatedValue;

                // Quy định tài sản công (TT 162/2014/TT-BTC): Giá trị còn lại tối thiểu của sách là 10.000 VNĐ
                const decimal NominalResidualValue = 10000;
                if (remainingValue < NominalResidualValue)
                {
                    remainingValue = NominalResidualValue;
                    accumulatedValue = originalPrice - remainingValue;
                    accumulatedRate = (double)((accumulatedValue / originalPrice) * 100);
                }

                var resultItem = new DepreciationResultDto
                {
                    Title = b.Title,
                    Isbn = b.Isbn ?? $"S{b.Id:000}",
                    PurchaseDate = purchaseDate,
                    OriginalValue = originalPrice,
                    UsefulLifeYears = usefulLife,
                    AccumulatedDepreciationPercent = accumulatedRate,
                    AccumulatedDepreciationValue = accumulatedValue,
                    RemainingValue = remainingValue
                };

                DepreciationResults.Add(resultItem);
                totalOriginal += originalPrice;
                totalRemaining += remainingValue;
            }

            TotalOriginalValue = totalOriginal;
            TotalRemainingValue = totalRemaining;
        }

        private void LoadMockData()
        {
            var mockBooks = new List<BookDepreciationSourceDto>
            {
                new BookDepreciationSourceDto { Id = 1, Title = "Đắc Nhân Tâm", Isbn = "9781607967552", Price = 150000, PurchaseDate = DateTime.Now.AddYears(-4), UsefulLifeYears = 5, IsDigital = false },
                new BookDepreciationSourceDto { Id = 2, Title = "Tắt Đèn", Isbn = "9786042131971", Price = 80000, PurchaseDate = DateTime.Now.AddYears(-3), UsefulLifeYears = 5, IsDigital = false },
                new BookDepreciationSourceDto { Id = 3, Title = "Số Đỏ", Isbn = "9786042131988", Price = 95000, PurchaseDate = DateTime.Now.AddYears(-2), UsefulLifeYears = 5, IsDigital = false },
                new BookDepreciationSourceDto { Id = 4, Title = "Đất Rừng Phương Nam", Isbn = "9786042131995", Price = 110000, PurchaseDate = DateTime.Now.AddYears(-1), UsefulLifeYears = 5, IsDigital = false },
                new BookDepreciationSourceDto { Id = 5, Title = "Lược sử thời gian", Isbn = "9786042131999", Price = 250000, PurchaseDate = DateTime.Now, UsefulLifeYears = 5, IsDigital = false }
            };

            FilterAndCalculate(mockBooks);
        }

        private async Task ExportExcelAsync()
        {
            if (DepreciationResults.Count == 0)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("❌ Không có dữ liệu tính hao mòn để xuất!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }

            var sfd = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"BaoCao_HaoMon_TaiSan_ThuVien_{SelectedYear}.xlsx",
                Title = "Lưu báo cáo hao mòn tài sản cố định"
            };

            if (sfd.ShowDialog() == true)
            {
                IsBusy = true;
                StatusMessage = "Đang xuất báo cáo hao mòn tài sản Excel...";

                try
                {
                    string[] headers = new[]
                    {
                        "Mã Sách (ISBN)",
                        "Tên Đầu Sách",
                        "Ngày Mua",
                        "Nguyên Giá (VNĐ)",
                        "Thời gian SD (Năm)",
                        "Hao mòn lũy kế (%)",
                        "Hao mòn lũy kế (VNĐ)",
                        "Giá trị còn lại (VNĐ)"
                    };

                    Func<DepreciationResultDto, object[]> rowMapper = (item) => new object[]
                    {
                        item.Isbn,
                        item.Title,
                        item.FormattedPurchaseDate,
                        item.OriginalValue,
                        item.UsefulLifeYears,
                        item.AccumulatedDepreciationPercent,
                        item.AccumulatedDepreciationValue,
                        item.RemainingValue
                    };

                    await ExcelExportService.ExportToXlsxAsync(
                        DepreciationResults,
                        headers,
                        rowMapper,
                        sfd.FileName,
                        $"HaoMon_{SelectedYear}",
                        $"BÁO CÁO HAO MÒN TÀI SẢN CỐ ĐỊNH NĂM {SelectedYear} (Theo Thông tư 23/2023/TT-BTC)");

                    await AuditLogService.WriteLogAsync("Xuất Excel hao mòn", $"Xuất báo cáo khấu hao tài sản cố định năm {SelectedYear} ra file '{System.IO.Path.GetFileName(sfd.FileName)}' thành công với {DepreciationResults.Count} dòng dữ liệu", true);

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("🎉 Xuất báo cáo khấu hao tài sản biểu mẫu ClosedXML thành công!", "Xuất Excel thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
                catch (Exception ex)
                {
                    await AuditLogService.WriteLogAsync("Xuất Excel hao mòn", $"Lỗi xuất file báo cáo khấu hao tài sản năm {SelectedYear}: {ex.Message}", false);
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"❌ Lỗi trong quá trình xuất Excel: {ex.Message}", "Lỗi xuất file", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }

                IsBusy = false;
                StatusMessage = "";
            }
        }
    }

    public class BookDepreciationSourceDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Isbn { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime PurchaseDate { get; set; }
        public int UsefulLifeYears { get; set; }
        public bool IsDigital { get; set; }
    }

    public class DepreciationResultDto
    {
        public string Title { get; set; } = string.Empty;
        public string Isbn { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public decimal OriginalValue { get; set; }
        public int UsefulLifeYears { get; set; }
        public double AccumulatedDepreciationPercent { get; set; }
        public decimal AccumulatedDepreciationValue { get; set; }
        public decimal RemainingValue { get; set; }

        public string FormattedPurchaseDate => PurchaseDate.ToString("dd/MM/yyyy");
        public string OriginalValueText => OriginalValue.ToString("N0") + " VNĐ";
        public string AccumulatedDepreciationPercentText => AccumulatedDepreciationPercent.ToString("N0") + "%";
        public string AccumulatedDepreciationValueText => AccumulatedDepreciationValue.ToString("N0") + " VNĐ";
        public string RemainingValueText => RemainingValue.ToString("N0") + " VNĐ";
    }
}
