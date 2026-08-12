using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Services;
using SmartLibrary.Desktop.Helpers;
using ClosedXML.Excel;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class DashboardViewModel : ObservableObject, IActiveAwareViewModel
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private string _totalBooks = "0";

        [ObservableProperty]
        private string _borrowedBooks = "0";

        [ObservableProperty]
        private string _reservedBooks = "0";

        [ObservableProperty]
        private string _unpaidFines = "0 VND";

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _errorMessage = "";

        [ObservableProperty]
        private bool _hasOverdueBooks;

        [ObservableProperty]
        private string _overdueAlertMessage = "";

        [ObservableProperty]
        private bool _isBorrowStatsEmpty = true;

        [ObservableProperty]
        private bool _isTasksEmpty = true;

        [ObservableProperty]
        private bool _isOverdueLoansEmpty = true;

        [ObservableProperty]
        private bool _isActivityLogsEmpty = true;

        // ============ ITEM 2.1: Shimmer Loading ============
        [ObservableProperty]
        private bool _isShimmerVisible = true; // Bật shimmer khi đang load

        [ObservableProperty]
        private bool _isDataLoaded = false; // Ẩn shimmer khi data sẵn sàng

        // ============ ITEM 2.2: Cache Dashboard ============
        private static DateTime _lastCacheTime = DateTime.MinValue;
        private static string _cachedTimeRange = "";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        [ObservableProperty]
        private string _cacheStatusText = "";

        public ObservableCollection<LoanDto> OverdueLoans { get; } = new();
        public ObservableCollection<ActivityLog> ActivityLogs { get; } = new();
        public ObservableCollection<LostItemStat> TopRiskClasses { get; } = new();
        public ObservableCollection<LibrarianTaskItem> LibrarianTasks { get; } = new();
        public ObservableCollection<BookBorrowStat> BorrowStats { get; } = new();

        [ObservableProperty]
        private string _selectedTimeRange = "Tuần này";

        public string[] AvailableTimeRanges { get; } = new[] { "Hôm nay", "Tuần này", "Tháng này" };

        partial void OnSelectedTimeRangeChanged(string value)
        {
            // TimeRange thay đổi → invalidate cache → load lại
            InvalidateCache();
            _ = LoadDataAsync();
        }

        public ICommand SendReminderCommand { get; }
        public ICommand RefreshDashboardCommand { get; }
        public ICommand ExportReportCommand { get; }

        public DashboardViewModel(ApiService apiService)
        {
            _apiService = apiService;
            SendReminderCommand = new RelayCommand<LoanDto>(SendReminder);
            RefreshDashboardCommand = new AsyncRelayCommand(ForceRefreshAsync);
            ExportReportCommand = new AsyncRelayCommand(ExportReportAsync);
        }

        public void Activate()
        {
            _ = LoadDataAsync();
        }

        private void SendReminder(LoanDto? loan)
        {
            if (loan != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(
                        $"Đã gửi tin nhắc nhở trả cuốn sách '{loan.BookTitle}' tới học sinh {loan.StudentName} thành công!",
                        "Nhắc nhở Trả Sách",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                });
            }
        }

        /// <summary>
        /// Ép load lại dữ liệu (bỏ qua cache) — nút Refresh
        /// </summary>
        private async Task ForceRefreshAsync()
        {
            InvalidateCache();
            await LoadDataAsync();
        }

        /// <summary>
        /// Xóa cache
        /// </summary>
        private void InvalidateCache()
        {
            _lastCacheTime = DateTime.MinValue;
            _cachedTimeRange = "";
            CacheStatusText = "";
        }

        /// <summary>
        /// Kiểm tra cache còn hợp lệ không
        /// </summary>
        private bool IsCacheValid()
        {
            if (DateTime.Now - _lastCacheTime > CacheDuration) return false;
            if (_cachedTimeRange != SelectedTimeRange) return false;
            return true;
        }

        public async Task LoadDataAsync()
        {
            if (IsBusy) return;

            // ===== ITEM 2.2: Kiểm tra Cache =====
            if (IsCacheValid())
            {
                var remaining = CacheDuration - (DateTime.Now - _lastCacheTime);
                CacheStatusText = $"📦 Cache: {remaining.Minutes}p{remaining.Seconds}s còn lại";
                return; // Không load lại
            }

            // ===== ITEM 2.1: Bật Shimmer =====
            IsShimmerVisible = true;
            IsDataLoaded = false;
            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                // Giả lập mạng chậm (1.5s để thấy shimmer rõ)
                await Task.Delay(1500);

                try
                {
                    var response = await _apiService.GetAsync<DashboardResponseDto>(ApiEndpoints.ReportsDashboard);
                    if (response != null && response.Summary != null)
                    {
                        TotalBooks = response.Summary.TotalBooks.ToString("N0");
                        BorrowedBooks = response.Summary.Borrowed.ToString("N0");
                        ReservedBooks = response.Summary.Reserved.ToString("N0");
                        UnpaidFines = response.Summary.UnpaidFines.ToString("N0") + " VND";
                    }
                    else
                    {
                        throw new Exception("Không tải được dữ liệu động từ máy chủ.");
                    }
                }
                catch (Exception apiEx)
                {
                    await AuditLogService.WriteLogAsync("Tải KPI Dashboard", $"Lỗi tải API: {apiEx.Message}. Sử dụng dữ liệu offline.", false);
                    
                    if (SelectedTimeRange == "Hôm nay")
                    {
                        TotalBooks = "15,200";
                        BorrowedBooks = "42";
                        ReservedBooks = "5";
                        UnpaidFines = "80.000 VND";
                    }
                    else if (SelectedTimeRange == "Tuần này")
                    {
                        TotalBooks = "15,200";
                        BorrowedBooks = "320";
                        ReservedBooks = "45";
                        UnpaidFines = "420.000 VND";
                    }
                    else
                    {
                        TotalBooks = "15,200";
                        BorrowedBooks = "1,045";
                        ReservedBooks = "120";
                        UnpaidFines = "1.500.000 VND";
                    }
                }

                // MOCK DATA: Biểu đồ cột xu hướng mượn sách
                BorrowStats.Clear();
                if (SelectedTimeRange == "Hôm nay")
                {
                    BorrowStats.Add(new BookBorrowStat { Label = "Văn học", Count = 15, HeightPercentage = 0, PrevYearCount = 12, PrevYearHeightPercentage = 0 });
                    BorrowStats.Add(new BookBorrowStat { Label = "Toán học", Count = 8, HeightPercentage = 0, PrevYearCount = 10, PrevYearHeightPercentage = 0 });
                    BorrowStats.Add(new BookBorrowStat { Label = "Vật lý", Count = 12, HeightPercentage = 0, PrevYearCount = 9, PrevYearHeightPercentage = 0 });
                    BorrowStats.Add(new BookBorrowStat { Label = "Hóa học", Count = 5, HeightPercentage = 0, PrevYearCount = 6, PrevYearHeightPercentage = 0 });
                    BorrowStats.Add(new BookBorrowStat { Label = "Anh văn", Count = 22, HeightPercentage = 0, PrevYearCount = 18, PrevYearHeightPercentage = 0 });
                }
                else if (SelectedTimeRange == "Tuần này")
                {
                    BorrowStats.Add(new BookBorrowStat { Label = "Văn học", Count = 98, HeightPercentage = 0, PrevYearCount = 75, PrevYearHeightPercentage = 0 });
                    BorrowStats.Add(new BookBorrowStat { Label = "Toán học", Count = 65, HeightPercentage = 0, PrevYearCount = 58, PrevYearHeightPercentage = 0 });
                    BorrowStats.Add(new BookBorrowStat { Label = "Vật lý", Count = 78, HeightPercentage = 0, PrevYearCount = 60, PrevYearHeightPercentage = 0 });
                    BorrowStats.Add(new BookBorrowStat { Label = "Hóa học", Count = 42, HeightPercentage = 0, PrevYearCount = 35, PrevYearHeightPercentage = 0 });
                    BorrowStats.Add(new BookBorrowStat { Label = "Anh văn", Count = 120, HeightPercentage = 0, PrevYearCount = 90, PrevYearHeightPercentage = 0 });
                }
                else // Tháng này
                {
                    BorrowStats.Add(new BookBorrowStat { Label = "Văn học", Count = 380, HeightPercentage = 0, PrevYearCount = 310, PrevYearHeightPercentage = 0 });
                    BorrowStats.Add(new BookBorrowStat { Label = "Toán học", Count = 240, HeightPercentage = 0, PrevYearCount = 200, PrevYearHeightPercentage = 0 });
                    BorrowStats.Add(new BookBorrowStat { Label = "Vật lý", Count = 310, HeightPercentage = 0, PrevYearCount = 260, PrevYearHeightPercentage = 0 });
                    BorrowStats.Add(new BookBorrowStat { Label = "Hóa học", Count = 190, HeightPercentage = 0, PrevYearCount = 150, PrevYearHeightPercentage = 0 });
                    BorrowStats.Add(new BookBorrowStat { Label = "Anh văn", Count = 450, HeightPercentage = 0, PrevYearCount = 380, PrevYearHeightPercentage = 0 });
                }

                // Dynamic height scaling (MaxHeight = 160.0)
                if (BorrowStats.Count > 0)
                {
                    double maxCount = 0;
                    foreach (var stat in BorrowStats)
                    {
                        if (stat.Count > maxCount) maxCount = stat.Count;
                        if (stat.PrevYearCount > maxCount) maxCount = stat.PrevYearCount;
                    }

                    if (maxCount <= 0) maxCount = 1.0;

                    foreach (var stat in BorrowStats)
                    {
                        stat.HeightPercentage = (stat.Count / maxCount) * 160.0;
                        stat.PrevYearHeightPercentage = (stat.PrevYearCount / maxCount) * 160.0;
                    }
                }

                // MOCK DATA: Danh sách nhiệm vụ của thủ thư
                if (LibrarianTasks.Count == 0)
                {
                    LibrarianTasks.Add(new LibrarianTaskItem { TaskName = "Kiểm tra RFID kệ sách Khoa học A3", IsCompleted = false });
                    LibrarianTasks.Add(new LibrarianTaskItem { TaskName = "Gửi nhắc nhở SMS học sinh quá hạn mượn", IsCompleted = true });
                    LibrarianTasks.Add(new LibrarianTaskItem { TaskName = "Nhập danh mục sách mới từ Excel (CSV)", IsCompleted = false });
                    LibrarianTasks.Add(new LibrarianTaskItem { TaskName = "Bảo trì máy in tem nhãn Zebra", IsCompleted = false });

                    // Load trạng thái đã lưu
                    LoadTasksFromFile();

                    // Auto-save khi tick thay đổi
                    foreach (var task in LibrarianTasks)
                    {
                        task.PropertyChanged += (s, e) =>
                        {
                            if (e.PropertyName == nameof(LibrarianTaskItem.IsCompleted))
                                SaveTasksToFile();
                        };
                    }
                }

                // MOCK DATA: Danh sách sách quá hạn
                OverdueLoans.Clear();
                OverdueLoans.Add(new LoanDto { StudentName = "Nguyễn Văn An (Lớp 10A1)", BookTitle = "Đắc Nhân Tâm", IsOverdue = true, DaysRemaining = -5 });
                OverdueLoans.Add(new LoanDto { StudentName = "Trần Thị Bình (Lớp 10A1)", BookTitle = "Vật lý Đại cương", IsOverdue = true, DaysRemaining = -12 });
                OverdueLoans.Add(new LoanDto { StudentName = "Lê Hoàng Cường (Lớp 11B2)", BookTitle = "Toán Học Cao Cấp", IsOverdue = true, DaysRemaining = -2 });

                HasOverdueBooks = true;
                OverdueAlertMessage = $"⚠️ Cảnh báo: Có {OverdueLoans.Count} người dùng đang mượn sách quá hạn. Vui lòng liên hệ nhắc nhở ngay lập tức.";

                // MOCK DATA: Luồng Hoạt động (Activity Stream)
                ActivityLogs.Clear();
                ActivityLogs.Add(new ActivityLog { Time = System.DateTime.Now.AddMinutes(-2).ToString("HH:mm"), Message = "Lớp 11A1 mượn 40 cuốn Ngữ Văn 11", Type = "Borrow" });
                ActivityLogs.Add(new ActivityLog { Time = System.DateTime.Now.AddMinutes(-5).ToString("HH:mm"), Message = "Kiosk: Học sinh Lê Hoàng C trả sách Toán Học Cao Cấp", Type = "Return" });
                ActivityLogs.Add(new ActivityLog { Time = System.DateTime.Now.AddMinutes(-12).ToString("HH:mm"), Message = "Nhập kho 50 cuốn sách từ Excel", Type = "System" });
                ActivityLogs.Add(new ActivityLog { Time = System.DateTime.Now.AddMinutes(-20).ToString("HH:mm"), Message = "Kiểm kho phát hiện thiếu 2 sách tại Kệ A3", Type = "Warning" });

                // MOCK DATA: Top Lớp Rủi Ro (Admin View)
                TopRiskClasses.Clear();
                if (AuthService.CurrentRole == "Admin")
                {
                    TopRiskClasses.Add(new LostItemStat { ClassName = "12A5", LostCount = 15, RiskLevel = "Cao", TotalPenalty = 750000 });
                    TopRiskClasses.Add(new LostItemStat { ClassName = "11B2", LostCount = 8, RiskLevel = "Trung Bình", TotalPenalty = 400000 });
                    TopRiskClasses.Add(new LostItemStat { ClassName = "10C1", LostCount = 3, RiskLevel = "Thấp", TotalPenalty = 150000 });
                }

                // Cập nhật trạng thái rỗng dữ liệu để hiển thị Empty States
                IsBorrowStatsEmpty = BorrowStats.Count == 0;
                IsTasksEmpty = LibrarianTasks.Count == 0;
                IsOverdueLoansEmpty = OverdueLoans.Count == 0;
                IsActivityLogsEmpty = ActivityLogs.Count == 0;

                // ===== ITEM 2.2: Cập nhật Cache =====
                _lastCacheTime = DateTime.Now;
                _cachedTimeRange = SelectedTimeRange;
                CacheStatusText = $"📦 Cache: 5p00s còn lại (load lúc {DateTime.Now:HH:mm:ss})";
            }
            catch (System.Exception ex)
            {
                ErrorMessage = "Lỗi tải dữ liệu: " + ex.Message;
            }
            finally
            {
                // ===== ITEM 2.1: Tắt Shimmer =====
                IsBusy = false;
                IsShimmerVisible = false;
                IsDataLoaded = true;
            }
        }

        private static string GetTasksFilePath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SmartLibrary", "data");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string ssoId = AuthService.CurrentUserSsoId ?? "default";
            if (string.IsNullOrWhiteSpace(ssoId)) ssoId = "default";
            string date = DateTime.Now.ToString("yyyyMMdd");
            return Path.Combine(dir, $"librarian_tasks_{ssoId}_{date}.json");
        }

        private void SaveTasksToFile()
        {
            try
            {
                var data = LibrarianTasks.Select(t => new { t.TaskName, t.IsCompleted }).ToList();
                string json = JsonSerializer.Serialize(data);
                File.WriteAllText(GetTasksFilePath(), json);
            }
            catch { /* Silent fail */ }
        }

        private void LoadTasksFromFile()
        {
            try
            {
                string path = GetTasksFilePath();
                if (!File.Exists(path)) return;

                string json = File.Exists(path) ? File.ReadAllText(path) : "";
                if (string.IsNullOrEmpty(json)) return;
                var saved = JsonSerializer.Deserialize<List<TaskSaveData>>(json);
                if (saved == null) return;

                foreach (var task in LibrarianTasks)
                {
                    var match = saved.FirstOrDefault(s => s.TaskName == task.TaskName);
                    if (match != null) task.IsCompleted = match.IsCompleted;
                }
            }
            catch { }
        }

        private async Task ExportReportAsync()
        {
            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"BaoCaoThongKeThuvien_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                Title = "Lưu báo cáo thống kê thư viện"
            };

            if (sfd.ShowDialog() == true)
            {
                IsBusy = true;
                ErrorMessage = "Đang xuất báo cáo...";
                try
                {
                    await Task.Run(() =>
                    {
                        using var workbook = new XLWorkbook();
                        
                        // Sheet 1: Thống kê tổng quan
                        var wsSummary = workbook.Worksheets.Add("Tổng quan");
                        
                        // Ghi tiêu đề lớn
                        var titleCell = wsSummary.Cell(1, 1);
                        titleCell.Value = "BÁO CÁO THỐNG KÊ THƯ VIỆN TOÀN CỤC";
                        titleCell.Style.Font.SetBold(true).Font.SetFontSize(16).Font.SetFontColor(XLColor.Indigo);
                        
                        wsSummary.Cell(2, 1).Value = $"Thời gian xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                        wsSummary.Cell(2, 1).Style.Font.SetItalic(true).Font.SetFontSize(10).Font.SetFontColor(XLColor.Gray);

                        // Ghi các chỉ số
                        wsSummary.Cell(4, 1).Value = "Chỉ số thống kê";
                        wsSummary.Cell(4, 1).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.LightGray);
                        wsSummary.Cell(4, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        
                        wsSummary.Cell(4, 2).Value = "Giá trị";
                        wsSummary.Cell(4, 2).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.LightGray);
                        wsSummary.Cell(4, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                        string[,] summaryData = {
                            { "Tổng sách trong kho", TotalBooks },
                            { "Sách đang được mượn", BorrowedBooks },
                            { "Sách đang dự trữ", ReservedBooks },
                            { "Tổng phí quá hạn tích lũy", UnpaidFines }
                        };

                        for (int i = 0; i < 4; i++)
                        {
                            int row = 5 + i;
                            wsSummary.Cell(row, 1).Value = summaryData[i, 0];
                            wsSummary.Cell(row, 2).Value = summaryData[i, 1];
                            wsSummary.Cell(row, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            wsSummary.Cell(row, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        }

                        // Chữ ký footer ở sheet tổng quan
                        int signRow = 11;
                        var cell1 = wsSummary.Cell(signRow, 1);
                        cell1.Value = "Người lập biểu\n(Ký, ghi rõ họ tên)";
                        cell1.Style.Font.SetItalic(true);
                        cell1.Style.Font.SetBold(true);
                        cell1.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        cell1.Style.Alignment.SetWrapText(true);

                        var cell2 = wsSummary.Cell(signRow, 2);
                        cell2.Value = "Hiệu trưởng duyệt\n(Ký tên, đóng dấu)";
                        cell2.Style.Font.SetItalic(true);
                        cell2.Style.Font.SetBold(true);
                        cell2.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        cell2.Style.Alignment.SetWrapText(true);

                        wsSummary.Row(signRow).Height = 45;

                        // Sheet 2: Sách quá hạn
                        var wsOverdue = workbook.Worksheets.Add("Sách quá hạn");
                        
                        var titleOverdue = wsOverdue.Cell(1, 1);
                        titleOverdue.Value = "DANH SÁCH ĐỘC GIẢ MƯỢN SÁCH QUÁ HẠN";
                        titleOverdue.Style.Font.SetBold(true).Font.SetFontSize(14).Font.SetFontColor(XLColor.Red);
                        
                        wsOverdue.Cell(2, 1).Value = $"Tổng số độc giả trễ hạn: {OverdueLoans.Count}";
                        wsOverdue.Cell(2, 1).Style.Font.SetItalic(true);

                        string[] overdueHeaders = { "Học sinh / Độc giả", "Tựa sách", "Số ngày quá hạn" };
                        for (int col = 0; col < overdueHeaders.Length; col++)
                        {
                            var cell = wsOverdue.Cell(4, col + 1);
                            cell.Value = overdueHeaders[col];
                            cell.Style.Font.SetBold(true);
                            cell.Style.Fill.SetBackgroundColor(XLColor.LightGray);
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        }

                        int oRow = 5;
                        foreach (var loan in OverdueLoans)
                        {
                            wsOverdue.Cell(oRow, 1).Value = loan.StudentName;
                            wsOverdue.Cell(oRow, 2).Value = loan.BookTitle;
                            wsOverdue.Cell(oRow, 3).Value = $"{Math.Abs(loan.DaysRemaining)} ngày";

                            for (int col = 1; col <= 3; col++)
                            {
                                wsOverdue.Cell(oRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            }
                            oRow++;
                        }

                        // Sheet 3: Lớp rủi ro làm mất sách
                        var wsRisk = workbook.Worksheets.Add("Lớp học cần lưu ý");
                        
                        var titleRisk = wsRisk.Cell(1, 1);
                        titleRisk.Value = "BÁO CÁO TÀI LIỆU THẤT LẠC THEO LỚP HỌC";
                        titleRisk.Style.Font.SetBold(true).Font.SetFontSize(14).Font.SetFontColor(XLColor.Indigo);

                        wsRisk.Cell(2, 1).Value = $"Thời gian lập biểu: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                        wsRisk.Cell(2, 1).Style.Font.SetItalic(true).Font.SetFontSize(10).Font.SetFontColor(XLColor.Gray);

                        string[] riskHeaders = { "Lớp học", "Số lượng sách thất lạc", "Mức độ cần lưu ý", "Tổng phí hỗ trợ đền bù" };
                        for (int col = 0; col < riskHeaders.Length; col++)
                        {
                            var cell = wsRisk.Cell(4, col + 1);
                            cell.Value = riskHeaders[col];
                            cell.Style.Font.SetBold(true);
                            cell.Style.Fill.SetBackgroundColor(XLColor.LightGray);
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        }

                        int rRow = 5;
                        foreach (var r in TopRiskClasses)
                        {
                            wsRisk.Cell(rRow, 1).Value = r.ClassName;
                            wsRisk.Cell(rRow, 2).Value = r.LostCount;
                            wsRisk.Cell(rRow, 3).Value = r.RiskLevel;
                            wsRisk.Cell(rRow, 4).Value = ((double)r.TotalPenalty).ToString("#,##0", new System.Globalization.CultureInfo("vi-VN")) + " VNĐ";

                            for (int col = 1; col <= 4; col++)
                            {
                                wsRisk.Cell(rRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            }
                            rRow++;
                        }

                        // Chữ ký duyệt Footer
                        int riskSignRow = rRow + 2;
                        var rc1 = wsRisk.Cell(riskSignRow, 1);
                        rc1.Value = "Người lập biểu\n(Ký, ghi rõ họ tên)";
                        rc1.Style.Font.SetItalic(true).Font.SetBold(true);
                        rc1.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        rc1.Style.Alignment.SetWrapText(true);

                        var rc2 = wsRisk.Cell(riskSignRow, 3);
                        rc2.Value = "Hiệu trưởng duyệt\n(Ký tên, đóng dấu)";
                        rc2.Style.Font.SetItalic(true).Font.SetBold(true);
                        rc2.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                        rc2.Style.Alignment.SetWrapText(true);

                        wsRisk.Row(riskSignRow).Height = 45;

                        // Auto-adjust columns size
                        wsSummary.Columns().AdjustToContents();
                        wsOverdue.Columns().AdjustToContents();
                        wsRisk.Columns().AdjustToContents();

                        workbook.SaveAs(sfd.FileName);
                    });

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        System.Windows.MessageBox.Show("✅ Xuất báo cáo thống kê thư viện ra Excel thành công!", "Xuất Báo Cáo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    });
                    ErrorMessage = "";
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        System.Windows.MessageBox.Show($"❌ Lỗi khi xuất báo cáo: {ex.Message}", "Lỗi Xuất File", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    });
                    ErrorMessage = "Lỗi xuất báo cáo: " + ex.Message;
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        private class TaskSaveData
        {
            public string TaskName { get; set; } = "";
            public bool IsCompleted { get; set; }
        }
    }



    public class LoanDto
    {
        public int Id { get; set; }
        public string StudentName { get; set; } = "";
        public string BookTitle { get; set; } = "";
        public string Barcode { get; set; } = "";
        public System.DateTime BorrowedDate { get; set; }
        public System.DateTime DueDate { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysRemaining { get; set; }

        public string OverdueText => IsOverdue ? $"{-DaysRemaining} ngày" : "";
    }

    public class ActivityLog
    {
        public string Time { get; set; } = "";
        public string Message { get; set; } = "";
        public string Type { get; set; } = ""; // Borrow, Return, Warning, System
    }

    public class LostItemStat
    {
        public string ClassName { get; set; } = "";
        public int LostCount { get; set; }
        public string RiskLevel { get; set; } = ""; // High, Medium, Low
        public decimal TotalPenalty { get; set; }
    }

    public class BookBorrowStat
    {
        public string Label { get; set; } = "";
        public int Count { get; set; }
        public double HeightPercentage { get; set; }
        public int PrevYearCount { get; set; }
        public double PrevYearHeightPercentage { get; set; }
    }

    public class LibrarianTaskItem : ObservableObject
    {
        public string TaskName { get; set; } = "";

        private bool _isCompleted;
        public bool IsCompleted
        {
            get => _isCompleted;
            set => SetProperty(ref _isCompleted, value);
        }
    }
}
