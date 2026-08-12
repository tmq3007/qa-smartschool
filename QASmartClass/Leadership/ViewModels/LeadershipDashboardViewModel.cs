using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;

namespace QASmartClass.Leadership.ViewModels
{
    public partial class LeadershipDashboardViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _welcomeMessage = "Dashboard Ban Giám Hiệu";

        [ObservableProperty]
        private int _totalStudents;

        [ObservableProperty]
        private int _totalTeachers;

        [ObservableProperty]
        private string _attendanceRate = "100%";

        [ObservableProperty]
        private int _totalIncidents;

        [ObservableProperty]
        private int _totalSkkns;

        [ObservableProperty]
        private ApprovalQueueViewModel _approvalQueueVM = new();

        public LeadershipDashboardViewModel()
        {
            _ = LoadKpiDataAsync();
        }

        [RelayCommand]
        private async Task LoadKpiDataAsync()
        {
            try
            {
                using var db = new AppDbContext();
                
                // Load data asynchronously to prevent UI blocking
                TotalStudents = await db.Students.CountAsync();
                TotalTeachers = await db.TeacherProfiles.CountAsync();
                
                // Count incidents
                TotalIncidents = await db.EventLogs.CountAsync(l => l.EventType == "Incident");
                
                // SKKN Count
                TotalSkkns = await db.Skkns.CountAsync();
                
                // Attendance rate calculation
                var today = DateTime.Today;
                var totalAttendanceRecords = await db.AttendanceRecords.CountAsync(a => a.Date == today);
                
                if (totalAttendanceRecords > 0)
                {
                    var presentCount = await db.AttendanceRecords.CountAsync(a => a.Date == today && (a.Status == "Present" || a.Status == "Có mặt"));
                    double rate = (double)presentCount / totalAttendanceRecords * 100;
                    AttendanceRate = $"{rate:0.0}%";
                }
                else
                {
                    // If no attendance records for today
                    AttendanceRate = "Chưa có dữ liệu";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi tải KPI Leadership: {ex.Message}");
                TotalStudents = 0;
                TotalTeachers = 0;
                TotalIncidents = 0;
                AttendanceRate = "0%";
            }
        }

        [RelayCommand]
        private async Task ExportReportAsync()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = $"Leadership_Report_{DateTime.Now:yyyyMMdd}.pdf"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using var db = new AppDbContext();
                    var pdfService = new QASmartClass.Services.PdfExportService(db);
                    
                    var report = new QASmartClass.Services.UsageReportDto
                    {
                        FromDate = DateTime.Today.AddDays(-30),
                        ToDate = DateTime.Today,
                        TotalSessions = await db.EventLogs.CountAsync(l => l.EventType == "Login" || l.EventType == "AppStart"),
                        TotalHours = 150.5,
                        TotalQuizzes = await db.EventLogs.CountAsync(l => l.EventType == "QuizSubmit"),
                        TotalFileTransfers = await db.EventLogs.CountAsync(l => l.EventType == "FileTransfer")
                    };
                    
                    string tempPath = pdfService.ExportUsageReport(report);
                    
                    if (!string.IsNullOrEmpty(tempPath) && System.IO.File.Exists(tempPath))
                    {
                        System.IO.File.Copy(tempPath, dialog.FileName, true);
                        System.Windows.MessageBox.Show($"Xuất thành công tại:\n{dialog.FileName}", "Thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Có lỗi xảy ra khi tạo PDF.", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Lỗi xuất báo cáo: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
    }
}

