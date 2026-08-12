using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Models;
using SmartLibrary.Desktop.Services;
using SmartLibrary.Desktop.Helpers;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class SystemLogViewModel : ObservableObject, IActiveAwareViewModel
    {
        public ObservableCollection<AuditLogDto> FilteredLogs { get; } = new();

        [ObservableProperty]
        private string _searchKeyword = "";

        [ObservableProperty]
        private string _selectedStatus = "Tất cả";

        [ObservableProperty]
        private bool _isExporting = false;

        [ObservableProperty]
        private string _exportStatusText = "";

        // Item 1.3: Hiển thị số pending sync
        [ObservableProperty]
        private string _pendingSyncText = "";

        [ObservableProperty]
        private bool _onlySupervisorOverride = false;

        [ObservableProperty]
        private DateTime? _startDate;

        [ObservableProperty]
        private DateTime? _endDate;

        public ObservableCollection<string> Statuses { get; } = new() { "Tất cả", "Thành công", "Thất bại" };

        public ICommand ExportLogCommand { get; }
        public ICommand SyncPendingCommand { get; }
        public ICommand ArchiveLogCommand { get; }

        public SystemLogViewModel()
        {
            ExportLogCommand = new AsyncRelayCommand(ExportLogAsync, () => FilteredLogs.Count > 0);
            SyncPendingCommand = new AsyncRelayCommand(SyncPendingAsync);
            ArchiveLogCommand = new AsyncRelayCommand(ArchiveLogAsync);

            ApplyFilters();
            UpdatePendingCount();

            // Auto-filter on selection or keyword change
            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SearchKeyword) ||
                    e.PropertyName == nameof(SelectedStatus) ||
                    e.PropertyName == nameof(OnlySupervisorOverride) ||
                    e.PropertyName == nameof(StartDate) ||
                    e.PropertyName == nameof(EndDate))
                {
                    ApplyFilters();
                }
            };

            // Listen to new logs added to the global service list
            AuditLogService.Logs.CollectionChanged += OnGlobalLogsChanged;
        }

        private void UpdatePendingCount()
        {
            PendingSyncText = AuditLogService.PendingSyncCount > 0
                ? $"⚠️ {AuditLogService.PendingSyncCount} log chờ đồng bộ"
                : "✅ Đã đồng bộ";
        }

        private async Task SyncPendingAsync()
        {
            try
            {
                ExportStatusText = "Đang đồng bộ...";
                int synced = await AuditLogService.SyncPendingLogsAsync();
                ExportStatusText = synced > 0
                    ? $"✅ Đã đồng bộ {synced} log lên server"
                    : "Không có log chờ đồng bộ";
                UpdatePendingCount();
            }
            catch (Exception ex)
            {
                ExportStatusText = $"❌ Lỗi đồng bộ: {ex.Message}";
            }
        }

        private void OnGlobalLogsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Re-apply filters on UI thread to ensure it syncs correctly
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                ApplyFilters();
                UpdatePendingCount();
            });
        }

        private void ApplyFilters()
        {
            FilteredLogs.Clear();
            var query = AuditLogService.Logs.AsEnumerable();

            // Filter by Search Keyword
            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                string kw = InputHelper.RemoveDiacritics(SearchKeyword.Trim()).ToLower();
                query = query.Where(l =>
                    (l.SsoUserId != null && InputHelper.RemoveDiacritics(l.SsoUserId).ToLower().Contains(kw)) ||
                    (l.ActionType != null && InputHelper.RemoveDiacritics(l.ActionType).ToLower().Contains(kw)) ||
                    (l.Details != null && InputHelper.RemoveDiacritics(l.Details).ToLower().Contains(kw))
                );
            }

            // Filter by Status
            if (SelectedStatus == "Thành công")
            {
                query = query.Where(l => l.Status == "Success");
            }
            else if (SelectedStatus == "Thất bại")
            {
                query = query.Where(l => l.Status == "Failure");
            }

            // Filter by Supervisor Override
            if (OnlySupervisorOverride)
            {
                query = query.Where(l => l.ActionType == "Supervisor Override" || (l.ActionType != null && l.ActionType.Contains("Giám thị")));
            }

            // Filter by Date Range
            if (StartDate.HasValue)
            {
                query = query.Where(l => l.Timestamp.Date >= StartDate.Value.Date);
            }
            if (EndDate.HasValue)
            {
                query = query.Where(l => l.Timestamp.Date <= EndDate.Value.Date);
            }

            foreach (var log in query)
            {
                FilteredLogs.Add(log);
            }
            (ExportLogCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Xuất nhật ký ra file Excel (CSV UTF-8 BOM) — Item 2.5
        /// </summary>
        private async Task ExportLogAsync()
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv",
                FileName = $"NhatKy_HeThong_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveDialog.ShowDialog() != true) return;

            IsExporting = true;
            ExportStatusText = "Đang xuất...";

            try
            {
                var headers = new[] { "ID", "Thời gian", "Người thực hiện", "Loại hành động", "Chi tiết", "Trạng thái" };

                await ExcelExportService.ExportToXlsxAsync(
                    FilteredLogs,
                    headers,
                    log => new object[]
                    {
                        log.Id,
                        log.Timestamp.ToString("dd/MM/yyyy HH:mm:ss"),
                        log.SsoUserId ?? "",
                        log.ActionType ?? "",
                        log.Details ?? "",
                        log.Status ?? ""
                    },
                    saveDialog.FileName,
                    "Nhật ký hệ thống"
                );

                await AuditLogService.WriteLogAsync("Xuất báo cáo",
                    $"Xuất nhật ký hệ thống ({FilteredLogs.Count} dòng) ra file: {Path.GetFileName(saveDialog.FileName)}", true);

                ExportStatusText = $"✅ Đã xuất {FilteredLogs.Count} dòng";
                System.Windows.MessageBox.Show(
                    $"Đã xuất thành công {FilteredLogs.Count} dòng nhật ký!\n\nFile: {saveDialog.FileName}",
                    "Thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ExportStatusText = $"❌ Lỗi: {ex.Message}";
                System.Windows.MessageBox.Show($"Lỗi xuất file: {ex.Message}", "Lỗi");
            }
            finally
            {
                IsExporting = false;
            }
        }

        private async Task ArchiveLogAsync()
        {
            var result = System.Windows.MessageBox.Show(
                "Bạn có chắc chắn muốn lưu trữ và xóa toàn bộ nhật ký hệ thống cục bộ không?\n\nHành động này sẽ đóng gói toàn bộ nhật ký hiện tại vào một tệp nén (.zip) để lưu trữ và dọn sạch nhật ký hiển thị trên phần mềm để tăng tốc độ.",
                "Xác nhận lưu trữ và xóa",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result != System.Windows.MessageBoxResult.Yes) return;

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Tệp nén Zip (*.zip)|*.zip",
                FileName = $"NhatKy_LuuTru_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveDialog.ShowDialog() != true) return;

            IsExporting = true;
            ExportStatusText = "Đang lưu trữ và xóa...";

            try
            {
                await AuditLogService.ArchiveAndClearLogsAsync(saveDialog.FileName);

                await AuditLogService.WriteLogAsync("Lưu trữ nhật ký",
                    $"Đã đóng gói và xóa nhật ký hệ thống cục bộ vào tệp: {Path.GetFileName(saveDialog.FileName)}", true);

                ExportStatusText = "✅ Lưu trữ thành công và đã dọn sạch";
                System.Windows.MessageBox.Show(
                    $"Đã lưu trữ và xóa sạch nhật ký thành công!\n\nFile lưu trữ: {saveDialog.FileName}",
                    "Thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ExportStatusText = $"❌ Lỗi: {ex.Message}";
                System.Windows.MessageBox.Show($"Lỗi lưu trữ: {ex.Message}", "Lỗi");
            }
            finally
            {
                IsExporting = false;
            }
        }

        public void Cleanup()
        {
            AuditLogService.Logs.CollectionChanged -= OnGlobalLogsChanged;
        }

        public void Activate()
        {
            AuditLogService.Logs.CollectionChanged -= OnGlobalLogsChanged;
            AuditLogService.Logs.CollectionChanged += OnGlobalLogsChanged;
            ApplyFilters();
        }
    }
}
