using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SmartLibrary.Desktop.Services;
using SmartLibrary.Desktop.Helpers;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class UsageReportsViewModel : ObservableObject, IActiveAwareViewModel
    {
        private readonly ApiService _apiService;

        public void Activate()
        {
            _ = LoadStatsAsync();
        }

        [ObservableProperty]
        private int _totalBooks = 0;

        [ObservableProperty]
        private int _borrowedBooks = 0;

        [ObservableProperty]
        private int _overdueBooks = 0;

        [ObservableProperty]
        private decimal _totalRevenue = 0;

        [ObservableProperty]
        private int _totalEbooksRead = 0;

        [ObservableProperty]
        private int _totalAudiobooksHeard = 0;

        [ObservableProperty]
        private double _paperSavedGrams = 0;

        public string PaperSavedText => PaperSavedGrams >= 1000 
            ? (PaperSavedGrams / 1000.0).ToString("N1") + " kg" 
            : PaperSavedGrams.ToString("N0") + " g";

        partial void OnPaperSavedGramsChanged(double value)
        {
            OnPropertyChanged(nameof(PaperSavedText));
        }

        [ObservableProperty]
        private double _treesSaved = 0;

        [ObservableProperty]
        private bool _isBusy = false;

        public ObservableCollection<ChartBarItem> DailyBorrowCounts { get; } = new();

        public ICommand LoadStatsCommand { get; }
        public ICommand ExportReportCommand { get; }

        public UsageReportsViewModel(ApiService apiService)
        {
            _apiService = apiService;
            LoadStatsCommand = new AsyncRelayCommand(LoadStatsAsync);
            ExportReportCommand = new AsyncRelayCommand(ExportReportAsync);
        }

        public async Task LoadStatsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var response = await _apiService.GetAsync<DashboardResponseDto>(ApiEndpoints.ReportsDashboard);
                if (response == null || response.Summary == null)
                {
                    throw new Exception("Dữ liệu trống từ máy chủ.");
                }

                TotalBooks = response.Summary.TotalBooks;
                BorrowedBooks = response.Summary.Borrowed;
                OverdueBooks = response.Summary.Overdue;
                TotalRevenue = response.Summary.Revenue;

                DailyBorrowCounts.Clear();
                if (response.ChartData != null && response.ChartData.Count > 0)
                {
                    int maxVal = response.ChartData.Max(d => d.Count);
                    maxVal = Math.Max(maxVal, 1);

                    foreach (var d in response.ChartData)
                    {
                        DailyBorrowCounts.Add(new ChartBarItem
                        {
                            DateLabel = d.Date.ToString("dd/MM"),
                            Count = d.Count,
                            BarHeight = ((double)d.Count / maxVal) * 150.0
                        });
                    }
                }

                // Query ESG metrics
                try
                {
                    var esgResponse = await _apiService.GetAsync<EsgMetricsDto>(ApiEndpoints.ReportsEsgMetrics);
                    if (esgResponse != null)
                    {
                        TotalEbooksRead = esgResponse.TotalEbooksRead;
                        TotalAudiobooksHeard = esgResponse.TotalAudiobooksHeard;
                        PaperSavedGrams = esgResponse.PaperSavedGrams;
                        TreesSaved = esgResponse.TreesSaved;
                    }
                }
                catch (Exception ex)
                {
                    await AuditLogService.WriteLogAsync("Tải ESG Metrics", $"Lỗi tải dữ liệu chỉ số ESG từ máy chủ: {ex.Message}. Sử dụng dữ liệu offline.", false);
                    LoadMockEsgStats();
                }
            }
            catch (Exception ex)
            {
                await AuditLogService.WriteLogAsync("Tải báo cáo thống kê", $"Lỗi tải dữ liệu thống kê từ máy chủ: {ex.Message}. Sử dụng dữ liệu offline.", false);
                LoadMockStats();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void LoadMockStats()
        {
            TotalBooks = 1250;
            BorrowedBooks = 380;
            OverdueBooks = 14;
            TotalRevenue = 3250000;
            LoadMockEsgStats();
            LoadMockChartData();
        }

        private void LoadMockEsgStats()
        {
            TotalEbooksRead = 1250;
            TotalAudiobooksHeard = 840;
            PaperSavedGrams = 125084.0;
            TreesSaved = 1.564;
        }

        private void LoadMockChartData()
        {
            DailyBorrowCounts.Clear();
            var rng = new Random();
            int max = 15;
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i);
                int count = rng.Next(2, 16);
                DailyBorrowCounts.Add(new ChartBarItem
                {
                    DateLabel = date.ToString("dd/MM"),
                    Count = count,
                    BarHeight = ((double)count / max) * 150.0
                });
            }
        }

        private async Task ExportReportAsync()
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"BaoCaoMuonTra_{DateTime.Now:yyyyMMdd}",
                Title = "Lưu báo cáo mượn trả"
            };

            if (saveDialog.ShowDialog() != true) return;

            IsBusy = true;
            try
            {
                var data = await _apiService.GetAsync<ActiveLoanItemDto[]>(ApiEndpoints.CirculationActiveLoans);
                var list = new List<ActiveLoanItemDto>();
                if (data == null)
                {
                    throw new Exception("Không thể tải danh sách mượn sách hoạt động từ máy chủ.");
                }
                list.AddRange(data);

                string[] headers = { "Mã Học Sinh", "Tên Học Sinh", "Tựa Đề Sách", "Mã Vạch", "Ngày Mượn", "Hạn Trả", "Số Ngày Còn Lại/Trễ", "Trạng Thái" };
                
                Func<ActiveLoanItemDto, object[]> rowMapper = item => new object[]
                {
                    item.StudentSsoId,
                    item.StudentName,
                    item.BookTitle,
                    item.Barcode,
                    item.BorrowedDate.ToString("dd/MM/yyyy"),
                    item.DueDate.ToString("dd/MM/yyyy"),
                    item.DaysRemaining,
                    item.IsOverdue ? "Cần hoàn trả ⏳" : "Đang mượn ✔️"
                };

                await ExcelExportService.ExportToXlsxAsync(
                    list,
                    headers,
                    rowMapper,
                    saveDialog.FileName,
                    "MuonTra_HoatDong",
                    "BÁO CÁO HOẠT ĐỘNG MƯỢN TRẢ SÁCH THƯ VIỆN");

                await AuditLogService.WriteLogAsync("Xuất Excel mượn trả", $"Xuất báo cáo hoạt động mượn trả ra file '{System.IO.Path.GetFileName(saveDialog.FileName)}' thành công với {list.Count} dòng dữ liệu", true);

                MessageBox.Show("Xuất báo cáo Excel thành công!", "Xuất Excel", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                await AuditLogService.WriteLogAsync("Xuất Excel mượn trả", $"Gặp sự cố khi xuất Excel: {ex.Message}", false);
                MessageBox.Show("Có lỗi xảy ra khi xuất Excel: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private List<ActiveLoanItemDto> GetMockActiveLoans()
        {
            return new List<ActiveLoanItemDto>
            {
                new ActiveLoanItemDto
                {
                    StudentSsoId = "HS001",
                    StudentName = "Nguyễn Văn An",
                    BookTitle = "Số Đỏ",
                    Barcode = "ISBN-9781607967552-1",
                    BorrowedDate = DateTime.Now.AddDays(-14),
                    DueDate = DateTime.Now.AddDays(-2),
                    DaysRemaining = -2,
                    IsOverdue = true
                },
                new ActiveLoanItemDto
                {
                    StudentSsoId = "HS002",
                    StudentName = "Võ Minh Em",
                    BookTitle = "Nhà Giả Kim",
                    Barcode = "ISBN-9786042131971-1",
                    BorrowedDate = DateTime.Now.AddDays(-5),
                    DueDate = DateTime.Now.AddDays(9),
                    DaysRemaining = 9,
                    IsOverdue = false
                }
            };
        }
    }

    public class DashboardResponseDto
    {
        public DashboardSummaryDto? Summary { get; set; }
        public List<DashboardChartItemDto>? ChartData { get; set; }
    }

    public class DashboardSummaryDto
    {
        public int TotalBooks { get; set; }
        public int Borrowed { get; set; }
        public int Overdue { get; set; }
        public decimal Revenue { get; set; }
        public int Reserved { get; set; }
        public decimal UnpaidFines { get; set; }
    }

    public class DashboardChartItemDto
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public class ActiveLoanItemDto
    {
        public int LoanId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentSsoId { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public DateTime BorrowedDate { get; set; }
        public DateTime DueDate { get; set; }
        public int DaysRemaining { get; set; }
        public bool IsOverdue { get; set; }
        public int RenewCount { get; set; }
    }

    public class ChartBarItem
    {
        public string DateLabel { get; set; } = "";
        public int Count { get; set; }
        public double BarHeight { get; set; }
    }

    public class EsgMetricsDto
    {
        public int TotalEbooksRead { get; set; }
        public int TotalAudiobooksHeard { get; set; }
        public double PaperSavedGrams { get; set; }
        public double TreesSaved { get; set; }
    }
}
