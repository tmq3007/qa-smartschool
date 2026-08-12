using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Services;
using SmartLibrary.Desktop.Helpers;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class AccessionRegisterViewModel : ObservableObject, IActiveAwareViewModel
    {
        public class AccessionRecordDto
        {
            public string AccessionNumber { get; set; } = string.Empty;
            public string Barcode { get; set; } = string.Empty;
            public string BookTitle { get; set; } = string.Empty;
            public string Author { get; set; } = string.Empty;
            public string PublisherName { get; set; } = string.Empty;
            public int PublishYear { get; set; }
            public decimal Price { get; set; }
            public string Source { get; set; } = "Mua";
            public string Status { get; set; } = "Trong kho";
            public DateTime DateAcquired { get; set; }
            
            public string PriceText => Price.ToString("N0") + " VNĐ";
            public string DateText => DateAcquired.ToString("dd/MM/yyyy");
        }

        // Full dataset
        private readonly List<AccessionRecordDto> _allRecords = new();
        private readonly ApiService _apiService;

        // Debounce Timer - trì hoãn 300ms trước khi filter
        private DispatcherTimer? _debounceTimer;
        private const int DebounceDelayMs = 350;

        // Bindable list
        public ObservableCollection<AccessionRecordDto> Records { get; } = new();

        // Filters
        [ObservableProperty]
        private string _searchText = "";

        [ObservableProperty]
        private string _selectedStatus = "Tất cả";

        [ObservableProperty]
        private string _selectedPublisher = "Tất cả";

        [ObservableProperty]
        private string _selectedSource = "Tất cả";

        // Lists for filters
        public ObservableCollection<string> Statuses { get; } = new() { "Tất cả", "Trong kho", "Đã mượn", "Thất lạc", "Thanh lý" };
        public ObservableCollection<string> Publishers { get; } = new() { "Tất cả", "NXB Giáo Dục Việt Nam", "NXB Trẻ", "NXB Kim Đồng", "NXB Tổng Hợp" };
        public ObservableCollection<string> Sources { get; } = new() { "Tất cả", "Mua", "Dự án TH", "Tặng" };

        [ObservableProperty]
        private bool _isExporting = false;

        [ObservableProperty]
        private int _exportProgress = 0;

        [ObservableProperty]
        private decimal _totalAssetValue;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _pageSize = 20;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private int _totalRecordsCount;

        public ICommand FilterCommand { get; }
        public ICommand ExportExcelCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }

        public AccessionRegisterViewModel(ApiService apiService)
        {
            _apiService = apiService;
            FilterCommand = new RelayCommand(ApplyFilters);
            ExportExcelCommand = new AsyncRelayCommand(ExportExcelAsync, () => _filteredRecords.Count > 0);
            NextPageCommand = new RelayCommand(NextPage, () => CurrentPage < TotalPages);
            PrevPageCommand = new RelayCommand(PrevPage, () => CurrentPage > 1);

            SeedData();
            ApplyFilters();

            // Khởi tạo Debounce Timer
            _debounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(DebounceDelayMs)
            };
            _debounceTimer.Tick += (s, e) =>
            {
                _debounceTimer.Stop(); // Chỉ chạy 1 lần
                ApplyFilters();
            };

            // Auto-filter on selection change (với debounce cho SearchText)
            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SearchText))
                {
                    // Debounce: Reset timer mỗi khi gõ phím
                    _debounceTimer?.Stop();
                    _debounceTimer?.Start();
                }
                else if (e.PropertyName == nameof(SelectedStatus) ||
                         e.PropertyName == nameof(SelectedPublisher) ||
                         e.PropertyName == nameof(SelectedSource))
                {
                    // ComboBox thay đổi → filter ngay (không cần debounce)
                    ApplyFilters();
                }
            };
        }

        public void Activate()
        {
            _ = LoadDataFromServerAsync();
        }

        private void SeedData()
        {
            _allRecords.Clear();
            string[] titles = {
                "Đắc Nhân Tâm", "Đường Cách Mệnh", "Truyện Kiều", "Số Đỏ", 
                "Tắt Đèn", "Lão Hạc", "Chí Phèo", "Vợ Nhặt", 
                "Dế Mèn Phiêu Lưu Ký", "Búp Sen Xanh", "Đất Rừng Phương Nam", "Mắt Biếc"
            };
            string[] authors = {
                "Dale Carnegie", "Nguyễn Ái Quốc", "Nguyễn Du", "Vũ Trọng Phụng",
                "Ngô Tất Tố", "Nam Cao", "Nam Cao", "Kim Lân",
                "Tô Hoài", "Sơn Tùng", "Đoàn Giỏi", "Nguyễn Nhật Ánh"
            };
            string[] pubs = { "NXB Trẻ", "NXB Giáo Dục Việt Nam", "NXB Kim Đồng", "NXB Tổng Hợp" };
            string[] sources = { "Mua", "Dự án TH", "Tặng" };

            var random = new Random(42);

            for (int i = 1; i <= 60; i++)
            {
                int titleIdx = i % titles.Length;
                decimal price = (random.Next(5, 15) * 10000);
                
                _allRecords.Add(new AccessionRecordDto
                {
                    AccessionNumber = $"DKCB-{i:0000}",
                    Barcode = $"S{i:000}-C01",
                    BookTitle = titles[titleIdx],
                    Author = authors[titleIdx],
                    PublisherName = pubs[i % pubs.Length],
                    PublishYear = 2018 + (i % 8),
                    Price = price,
                    Source = sources[i % sources.Length],
                    Status = i == 5 ? "Thất lạc" : (i % 12 == 0 ? "Đã mượn" : "Trong kho"),
                    DateAcquired = DateTime.Now.AddDays(-i * 3)
                });
            }
        }

        private void ApplyFilters()
        {
            CurrentPage = 1;
            UpdateFilteredList();
        }

        private List<AccessionRecordDto> _filteredRecords = new();

        private void UpdateFilteredList()
        {
            var query = _allRecords.AsEnumerable();

            // Search Keyword
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string kw = SmartLibrary.Desktop.Helpers.InputHelper.RemoveDiacritics(SearchText.Trim()).ToLower();
                query = query.Where(r => 
                    (r.AccessionNumber != null && SmartLibrary.Desktop.Helpers.InputHelper.RemoveDiacritics(r.AccessionNumber).ToLower().Contains(kw)) ||
                    (r.Barcode != null && SmartLibrary.Desktop.Helpers.InputHelper.RemoveDiacritics(r.Barcode).ToLower().Contains(kw)) ||
                    (r.BookTitle != null && SmartLibrary.Desktop.Helpers.InputHelper.RemoveDiacritics(r.BookTitle).ToLower().Contains(kw)) ||
                    (r.Author != null && SmartLibrary.Desktop.Helpers.InputHelper.RemoveDiacritics(r.Author).ToLower().Contains(kw))
                );
            }

            // Status Filter
            if (SelectedStatus != "Tất cả")
            {
                query = query.Where(r => r.Status.Equals(SelectedStatus, StringComparison.OrdinalIgnoreCase));
            }

            // Publisher Filter
            if (SelectedPublisher != "Tất cả")
            {
                query = query.Where(r => r.PublisherName.Equals(SelectedPublisher, StringComparison.OrdinalIgnoreCase));
            }

            // Source Filter
            if (SelectedSource != "Tất cả")
            {
                query = query.Where(r => r.Source.Equals(SelectedSource, StringComparison.OrdinalIgnoreCase));
            }

            _filteredRecords = query.ToList();
            TotalRecordsCount = _filteredRecords.Count;
            TotalAssetValue = _filteredRecords.Where(r => r.Status != "Thất lạc" && r.Status != "Thanh lý").Sum(r => r.Price);
            TotalPages = (int)Math.Ceiling((double)_filteredRecords.Count / PageSize);
            if (TotalPages == 0) TotalPages = 1;
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            DisplayCurrentPage();
            (ExportExcelCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
        }

        private void DisplayCurrentPage()
        {
            Records.Clear();
            var paged = _filteredRecords.Skip((CurrentPage - 1) * PageSize).Take(PageSize);
            foreach (var record in paged)
            {
                Records.Add(record);
            }
            (NextPageCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (PrevPageCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }

        private void NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                DisplayCurrentPage();
            }
        }

        private void PrevPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                DisplayCurrentPage();
            }
        }

        private async Task ExportExcelAsync()
        {
            _debounceTimer?.Stop();
            ApplyFilters();

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx|Comma Separated Values (*.csv)|*.csv",
                FileName = $"SoDKCB_BaoCao_Export_{DateTime.Now:yyyyMMdd}"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                IsExporting = true;
                ExportProgress = 0;

                try
                {
                    var headers = new[] { "Số ĐKCB", "Mã Barcode", "Tên sách", "Tác giả", "Nhà xuất bản", "Năm xuất bản", "Đơn giá", "Nguồn cấp", "Trạng thái", "Ngày nhập sổ" };

                    await ExcelExportService.ExportToXlsxWithProgressAsync(
                        _filteredRecords,
                        headers,
                        r => new object[] { r.AccessionNumber, r.Barcode, r.BookTitle, r.Author, r.PublisherName, r.PublishYear, r.Price, r.Source, r.Status, r.DateText },
                        saveFileDialog.FileName,
                        "SoDKCB",
                        progress => System.Windows.Application.Current.Dispatcher.Invoke(() => ExportProgress = progress),
                        "SỔ ĐĂNG KÝ CÁ BIỆT THƯ VIỆN TRƯỜNG"
                    );

                    string librarianId = AuthService.CurrentUserSsoId ?? "Thủ thư";
                    await AuditLogService.WriteLogAsync("Xuất báo cáo", $"Thủ thư [{librarianId}] xuất thành công Sổ ĐKCB ({_filteredRecords.Count} dòng) ra file: {Path.GetFileName(saveFileDialog.FileName)}", true);
                    System.Windows.MessageBox.Show($"Đã xuất thành công {_filteredRecords.Count} bản ghi Sổ đăng ký cá biệt ra file!\n\nFile: {saveFileDialog.FileName}", "Thông báo thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    string librarianId = AuthService.CurrentUserSsoId ?? "Thủ thư";
                    await AuditLogService.WriteLogAsync("Xuất báo cáo", $"Thủ thư [{librarianId}] xuất Sổ ĐKCB ra file thất bại: {ex.Message}", false);
                    System.Windows.MessageBox.Show($"Có lỗi xảy ra khi xuất file: {ex.Message}", "Lỗi hệ thống", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
                finally
                {
                    IsExporting = false;
                    ExportProgress = 0;
                }
            }
        }

        public async Task LoadDataFromServerAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var books = await _apiService.GetAsync<BookDto[]>(ApiEndpoints.Books);
                _allRecords.Clear();
                if (books != null)
                {
                    int count = 1;
                    foreach (var book in books)
                    {
                        if (book.Copies != null)
                        {
                            foreach (var copy in book.Copies)
                            {
                                _allRecords.Add(new AccessionRecordDto
                                {
                                    AccessionNumber = $"DKCB-{count++:0000}",
                                    Barcode = copy.Barcode,
                                    BookTitle = book.Title,
                                    Author = book.Author ?? "Vô danh",
                                    PublisherName = book.Publisher ?? "NXB Tổng Hợp",
                                    PublishYear = book.PublishYear > 0 ? book.PublishYear : 2024,
                                    Price = book.Price,
                                    Source = "Mua",
                                    Status = copy.Status == 0 ? "Trong kho" 
                                            : copy.Status == 1 ? "Đã mượn" 
                                            : copy.Status == 2 ? "Thất lạc" 
                                            : copy.Status == 3 ? "Thanh lý" 
                                            : "Trong kho",
                                    DateAcquired = DateTime.Now.AddDays(-count * 2)
                                });
                            }
                        }
                    }
                }
                ApplyFilters();
            }
            catch (Exception ex)
            {
                await AuditLogService.WriteLogAsync("Tải sổ ĐKCB", $"Lỗi kết nối máy chủ: {ex.Message}. Sử dụng dữ liệu bộ đệm hiện tại.", false);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
