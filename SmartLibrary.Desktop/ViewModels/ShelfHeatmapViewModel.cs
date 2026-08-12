using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Services;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class ShelfHeatmapViewModel : ObservableObject, IActiveAwareViewModel
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private string _selectedShelf = "Kệ A";

        public ObservableCollection<string> AvailableShelves { get; } = new() { "Kệ A", "Kệ B", "Kệ C", "Kệ D" };
        public ObservableCollection<ShelfGridCell> ShelfCells { get; } = new();

        public ICommand LoadHeatmapCommand { get; }
        public ICommand OptimizeDistributionCommand { get; }

        public ShelfHeatmapViewModel(ApiService apiService)
        {
            _apiService = apiService;
            LoadHeatmapCommand = new AsyncRelayCommand(LoadHeatmapAsync);
            OptimizeDistributionCommand = new RelayCommand(OptimizeDistribution);
        }

        public void Activate()
        {
            _ = LoadHeatmapAsync();
        }

        partial void OnSelectedShelfChanged(string value)
        {
            _ = LoadHeatmapAsync();
        }

        public async Task LoadHeatmapAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ShelfCells.Clear();

            try
            {
                // Gọi API lấy danh sách sách
                var books = await _apiService.GetAsync<BookDto[]>("/Books");
                if (books != null && books.Length > 0)
                {
                    // Lọc sách thuộc kệ được chọn bằng ShelfGridX
                    // Kệ A: X < 3, Kệ B: X ∈ [3, 6), Kệ C: X ∈ [6, 9), Kệ D: X >= 9
                    var shelfBooks = books.Where(b => {
                        string shelfName = b.ShelfGridX < 3 ? "Kệ A"
                                         : b.ShelfGridX < 6 ? "Kệ B"
                                         : b.ShelfGridX < 9 ? "Kệ C"
                                         : "Kệ D";
                        return shelfName == SelectedShelf;
                    }).ToList();
                    
                    // Tạo lưới 4x4 (4 hàng x 4 cột ngăn kệ)
                    for (int row = 1; row <= 4; row++)
                    {
                        for (int col = 1; col <= 4; col++)
                        {
                            // Lọc các sách nằm ở ngăn này
                            // Hàng = ShelfGridY % 4 + 1
                            // Cột = (ShelfLevel - 1) % 4 + 1
                            var matchedBooks = shelfBooks.Where(b => 
                                (b.ShelfGridY % 4 + 1) == row && 
                                ((b.ShelfLevel - 1) % 4 + 1) == col
                            ).ToList();

                            // Tính toán tần suất mượn để tạo heatmap level
                            int totalBorrows = matchedBooks.Sum(b => BookLookupService.GetLookupCount(b.Id));
                            if (matchedBooks.Count == 0) totalBorrows = 0;

                            string heatColor = GetHeatmapColor(totalBorrows);
                            string heatLevelText = totalBorrows > 35 ? "Rất Nóng 🔥" : totalBorrows > 15 ? "Trung Bình ⚡" : totalBorrows > 0 ? "Mát ❄️" : "Trống ❌";

                            ShelfCells.Add(new ShelfGridCell
                            {
                                Row = row - 1,
                                Column = col - 1,
                                LocationName = $"Ngăn {row}.{col}",
                                BookCount = matchedBooks.Count,
                                TotalBorrows = totalBorrows,
                                HeatColor = heatColor,
                                HeatLevelText = heatLevelText,
                                BooksSummary = matchedBooks.Count > 0 
                                    ? string.Join("\n", matchedBooks.Take(3).Select(b => $"• {b.Title} ({b.Author})")) 
                                    : "Không có sách"
                            });
                        }
                    }
                }
                else
                {
                    LoadMockHeatmap();
                }
            }
            catch (Exception ex)
            {
                await AuditLogService.WriteLogAsync("Tải bản đồ nhiệt kệ sách", $"Lỗi tải dữ liệu bản đồ nhiệt cho {SelectedShelf} từ máy chủ: {ex.Message}. Sử dụng dữ liệu offline.", false);
                LoadMockHeatmap();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private string GetHeatmapColor(int borrowCount)
        {
            if (borrowCount == 0) return "HeatmapEmptyBrush"; // Slate (Trống)
            if (borrowCount < 8) return "HeatmapColdBrush";  // Blue (Lạnh)
            if (borrowCount < 20) return "HeatmapCoolBrush"; // Green (Mát)
            if (borrowCount < 40) return "HeatmapWarmBrush"; // Amber (Ấm)
            return "HeatmapVeryHotBrush";                     // Red (Rất nóng)
        }

        private async void OptimizeDistribution()
        {
            var hotCells = ShelfCells.Where(c => c.TotalBorrows > 30).ToList();
            if (hotCells.Count == 0)
            {
                await AuditLogService.WriteLogAsync("Tối ưu hóa kệ sách AI", $"Chạy đề xuất tối ưu hóa phân bổ sách cho {SelectedShelf}. Kệ sách ở trạng thái cân bằng lý tưởng.", true);
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show("✅ Sơ đồ nhiệt kệ sách ở mức lý tưởng. Chưa cần tái cấu trúc phân bổ giá kệ!", "Phân tích phân bổ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                });
                return;
            }

            string details = "Các vị trí giá kệ đang ở mức Rất Nóng (Quá tải lượt tra cứu/mượn):\n";
            foreach (var cell in hotCells)
            {
                details += $"- {cell.LocationName}: {cell.TotalBorrows} lượt quan tâm.\n";
            }
            details += "\n💡 ĐỀ XUẤT CỦA HỆ THỐNG:\n1. Di dời bớt các đầu sách có lượt mượn cao ở các ngăn này sang các ngăn trống hơn như ngăn 3.4 hoặc 4.4.\n2. Bố trí thêm biển hướng dẫn và mở rộng lối đi tại các ngăn bị quá tải này.";

            await AuditLogService.WriteLogAsync("Tối ưu hóa kệ sách AI", $"Chạy đề xuất tối ưu hóa phân bổ sách cho {SelectedShelf}. Phát hiện {hotCells.Count} ngăn quá tải.", true);

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                System.Windows.MessageBox.Show(details, "Đề xuất Tối ưu hóa Kệ sách AI", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            });
        }

        private void LoadMockHeatmap()
        {
            var rng = new Random(SelectedShelf.GetHashCode());
            for (int row = 1; row <= 4; row++)
            {
                for (int col = 1; col <= 4; col++)
                {
                    int bookCount = rng.Next(0, 12);
                    int totalBorrows = bookCount == 0 ? 0 : rng.Next(1, 35);
                    string heatColor = GetHeatmapColor(totalBorrows);
                    string heatLevelText = totalBorrows > 20 ? "Rất Nóng 🔥" : totalBorrows > 10 ? "Trung Bình ⚡" : totalBorrows > 0 ? "Mát ❄️" : "Trống ❌";

                    string summary = "Không có sách";
                    if (bookCount > 0)
                    {
                        var mockTitles = new[] { "Số Đỏ", "Chí Phèo", "Búp Sen Xanh", "Đất Rừng Phương Nam", "Vợ Nhặt", "Tắt Đèn", "Lão Hạc" };
                        summary = string.Join("\n", Enumerable.Range(0, Math.Min(3, bookCount)).Select(i => $"• {mockTitles[rng.Next(mockTitles.Length)]}"));
                    }

                    ShelfCells.Add(new ShelfGridCell
                    {
                        Row = row - 1,
                        Column = col - 1,
                        LocationName = $"Ngăn {row}.{col}",
                        BookCount = bookCount,
                        TotalBorrows = totalBorrows,
                        HeatColor = heatColor,
                        HeatLevelText = heatLevelText,
                        BooksSummary = summary
                    });
                }
            }
        }
    }

    public class ShelfGridCell
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public int BookCount { get; set; }
        public int TotalBorrows { get; set; }
        public string HeatColor { get; set; } = string.Empty;
        public string HeatLevelText { get; set; } = string.Empty;
        public string BooksSummary { get; set; } = string.Empty;
    }
}
