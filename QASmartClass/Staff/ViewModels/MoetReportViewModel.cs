using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.Staff.Services;

namespace QASmartClass.Staff.ViewModels
{
    public partial class MoetReportViewModel : ObservableObject, IDisposable
    {
        private readonly MoetReportService _reportService;
        private readonly AppDbContext? _dbLocal;
        private readonly bool _ownsDb;
        private readonly ISaveFileDialogService _dialogService;

        [ObservableProperty]
        private bool _isBusy;

        public MoetReportViewModel() : this(null, null) { }

        public MoetReportViewModel(ISaveFileDialogService? dialogService, AppDbContext? db = null)
        {
            _dialogService = dialogService ?? new DefaultSaveFileDialogService();
            if (db != null)
            {
                _reportService = new MoetReportService(db);
                _ownsDb = false;
            }
            else if (AppServices.Database != null)
            {
                _reportService = new MoetReportService(AppServices.Database);
                _ownsDb = false;
            }
            else
            {
                _dbLocal = new AppDbContext();
                _reportService = new MoetReportService(_dbLocal);
                _ownsDb = true;
            }
        }

        [RelayCommand]
        private async Task ExportMau20PdfAsync() => await ExportReportWithFormatAsync("Mau20", "BaoCao_Mau20", "pdf");

        [RelayCommand]
        private async Task ExportMau20ExcelAsync() => await ExportReportWithFormatAsync("Mau20", "BaoCao_Mau20", "xlsx");

        [RelayCommand]
        private async Task ExportMau22PdfAsync() => await ExportReportWithFormatAsync("Mau22", "BaoCao_Mau22", "pdf");

        [RelayCommand]
        private async Task ExportMau22ExcelAsync() => await ExportReportWithFormatAsync("Mau22", "BaoCao_Mau22", "xlsx");

        [RelayCommand]
        private async Task ExportSummaryPdfAsync() => await ExportReportWithFormatAsync("Summary", "BaoCao_TongKet", "pdf");

        [RelayCommand]
        private async Task ExportSummaryExcelAsync() => await ExportReportWithFormatAsync("Summary", "BaoCao_TongKet", "xlsx");

        private async Task ExportReportWithFormatAsync(string reportType, string defaultBaseName, string format)
        {
            if (IsBusy) return;

            string filter = format == "pdf"
                ? "PDF Document (*.pdf)|*.pdf"
                : "Excel Workbook (*.xlsx)|*.xlsx";
            string defaultExt = format == "pdf" ? ".pdf" : ".xlsx";

            string? targetPath = _dialogService.ShowSaveFileDialog(defaultBaseName, filter, defaultExt);
            if (string.IsNullOrEmpty(targetPath)) return;

            try
            {
                IsBusy = true;
                string tempPath = "";

                if (format == "xlsx")
                {
                    tempPath = await Task.Run(() => 
                    {
                        return reportType switch
                        {
                            "Mau20" => _reportService.GenerateMau20Excel(),
                            "Mau22" => _reportService.GenerateMau22Excel(),
                            "Summary" => _reportService.GenerateSummaryReportExcel(),
                            _ => throw new ArgumentException("Loại báo cáo không hợp lệ")
                        };
                    });
                }
                else
                {
                    tempPath = await Task.Run(() => 
                    {
                        return reportType switch
                        {
                            "Mau20" => _reportService.GenerateMau20Pdf(),
                            "Mau22" => _reportService.GenerateMau22Pdf(),
                            "Summary" => _reportService.GenerateSummaryReportPdf(),
                            _ => throw new ArgumentException("Loại báo cáo không hợp lệ")
                        };
                    });
                }

                try
                {
                    await Task.Run(() => File.Copy(tempPath, targetPath, true));

                    try
                    {
                        using (var db = new AppDbContext())
                        {
                            string actor = StaffSession.CurrentUser?.TeacherCode ?? "StaffUser";
                            AuditHelper.Log(db, "Export_MOET_Report", actor, $"Exported MOET report {reportType} to {format} format: {Path.GetFileName(targetPath)}");
                        }
                    }
                    catch { }

                    _dialogService.ShowMessage("Xuất báo cáo thành công!", "Thành công", false);
                    _dialogService.StartProcess(targetPath);
                }
                finally
                {
                    if (!string.IsNullOrEmpty(tempPath))
                    {
                        try
                        {
                            await Task.Run(() => {
                                if (File.Exists(tempPath))
                                {
                                    File.Delete(tempPath);
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            Serilog.Log.Error(ex, "[MoetReport] Failed to delete temporary file at {Path}", tempPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage("Lỗi xuất báo cáo: " + ex.Message, "Lỗi", true);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void Dispose()
        {
            if (_ownsDb) _dbLocal?.Dispose();
        }
    }

    public class DefaultSaveFileDialogService : ISaveFileDialogService
    {
        public string? ShowSaveFileDialog(string defaultBaseName, string filter, string defaultExt)
        {
            bool isTestHost = System.Diagnostics.Process.GetCurrentProcess().ProcessName.Contains("testhost");
            if (isTestHost) return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{defaultBaseName}_test{defaultExt}");

            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = filter,
                DefaultExt = defaultExt,
                FileName = $"{defaultBaseName}_{DateTime.Now:yyyyMMdd}"
            };
            return sfd.ShowDialog() == true ? sfd.FileName : null;
        }

        public async void ShowMessage(string message, string title, bool isError = false)
        {
            bool isTestHost = System.Diagnostics.Process.GetCurrentProcess().ProcessName.Contains("testhost");
            if (!isTestHost)
            {
                await AppServices.UIService.ShowInfoAsync(message, title);
            }
        }

        public void StartProcess(string filePath)
        {
            bool isTestHost = System.Diagnostics.Process.GetCurrentProcess().ProcessName.Contains("testhost");
            if (!isTestHost)
            {
                new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true }
                }.Start();
            }
        }
    }
}
