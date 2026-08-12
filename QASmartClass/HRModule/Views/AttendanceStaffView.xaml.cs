using QASmartClass.Data;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace QASmartClass.HRModule.Views
{
    public partial class AttendanceStaffView : Page
    {
        private readonly AppDbContext _db;

        public AttendanceStaffView()
        {
            InitializeComponent();
            _db = new AppDbContext();
            Unloaded += (s, e) => { _db?.Dispose(); };
            Loaded += (_, __) => LoadData();
        }

        private void LoadData()
        {
            try
            {
                if (!_db.StaffAttendances.Any())
                {
                    _db.StaffAttendances.AddRange(
                        new StaffAttendance { StaffId = 1, Date = DateTime.Today, CheckIn = new TimeSpan(7, 30, 0), CheckOut = new TimeSpan(17, 0, 0), WorkingHours = 8.5, Status = "Present" },
                        new StaffAttendance { StaffId = 2, Date = DateTime.Today, CheckIn = new TimeSpan(8, 15, 0), CheckOut = new TimeSpan(17, 0, 0), WorkingHours = 7.75, Status = "Late" }
                    );
                    _db.SaveChanges();
                }

                DgAttendance.ItemsSource = _db.StaffAttendances.OrderByDescending(x => x.Date).ToList();
            }
            catch (Exception ex) { Log.Warning("Attendance Load error: {Err}", ex.Message); }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadData();

        private void BtnCalculateSalary_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int currentMonth = DateTime.Today.Month;
                int currentYear = DateTime.Today.Year;
                var staffList = _db.StaffProfiles.ToList();
                if (!staffList.Any())
                {
                    MessageBox.Show("Không có dữ liệu nhân sự để tính lương.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int count = 0;
                foreach (var staff in staffList)
                {
                    // Tính tổng số giờ làm việc trong tháng hiện tại của nhân sự
                    double totalHours = _db.StaffAttendances
                        .Where(x => x.StaffId == staff.Id && x.Date.Month == currentMonth && x.Date.Year == currentYear)
                        .Sum(x => x.WorkingHours);

                    decimal baseSalary = (decimal)staff.BaseSalary;
                    decimal deduction = 0;
                    if (totalHours < 208.0)
                    {
                        double missingHours = 208.0 - totalHours;
                        deduction = Math.Round((decimal)(missingHours * (staff.BaseSalary / 208.0)), 2);
                    }

                    var existRecord = _db.PayrollRecords.FirstOrDefault(p => 
                        p.StaffName == staff.FullName && 
                        p.Month == currentMonth && 
                        p.Year == currentYear);

                    if (existRecord != null)
                    {
                        existRecord.BaseSalary = baseSalary;
                        existRecord.Deduction = deduction;
                        existRecord.Allowance = 0;
                        existRecord.Tax = 0;
                    }
                    else
                    {
                        var pr = new PayrollRecord
                        {
                            StaffName = staff.FullName,
                            Month = currentMonth,
                            Year = currentYear,
                            BaseSalary = baseSalary,
                            Allowance = 0,
                            Deduction = deduction,
                            Tax = 0,
                            Status = "Draft"
                        };
                        _db.PayrollRecords.Add(pr);
                    }
                    count++;
                }

                _db.SaveChanges();
                MessageBox.Show($"Đã tính lương dựa trên chuyên cần tháng này cho {count} nhân sự.\nVui lòng xem báo cáo ở chức năng Xuất PDF.", "Tính Lương Tự Động", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Calculate salary failed");
                MessageBox.Show("Đã xảy ra lỗi khi tính lương: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int currentMonth = DateTime.Today.Month;
                int currentYear = DateTime.Today.Year;
                var payrolls = _db.PayrollRecords
                    .Where(p => p.Month == currentMonth && p.Year == currentYear)
                    .ToList();

                if (!payrolls.Any())
                {
                    MessageBox.Show("Vui lòng thực hiện tính lương trước khi xuất báo cáo PDF.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string exportsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "QASmartClass", "Exports");
                System.IO.Directory.CreateDirectory(exportsFolder);
                string fileName = Path.Combine(exportsFolder, $"BangLuong_{currentMonth}_{currentYear}.pdf");

                QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1.5f, QuestPDF.Infrastructure.Unit.Centimetre);
                        page.PageColor(QuestPDF.Helpers.Colors.White);
                        
                        page.Header().Column(col =>
                        {
                            col.Item().Text("SỞ GIÁO DỤC VÀ ĐÀO TẠO").Bold().FontSize(10);
                            col.Item().Text("TRƯỜNG THCS/THPT QA SMARTSCHOOL").Bold().FontSize(10);
                            col.Item().PaddingTop(15).AlignCenter().Text($"BẢNG LƯƠNG CHẤM CÔNG THÁNG {currentMonth}/{currentYear}").Bold().FontSize(16).FontColor(QuestPDF.Helpers.Colors.Blue.Darken3);
                            col.Item().PaddingTop(10);
                        });
                        
                        page.Content().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(40); // STT
                                columns.RelativeColumn(3);  // Họ và tên
                                columns.RelativeColumn(2);  // Lương cơ bản
                                columns.RelativeColumn(1.5f); // Giờ làm việc
                                columns.RelativeColumn(2);  // Khấu trừ
                                columns.RelativeColumn(2);  // Thực lĩnh
                            });
                            
                            table.Header(header =>
                            {
                                header.Cell().Border(1).Background(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).Text("STT").Bold().AlignCenter();
                                header.Cell().Border(1).Background(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).Text("Họ và Tên").Bold().AlignCenter();
                                header.Cell().Border(1).Background(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).Text("Lương cơ bản").Bold().AlignCenter();
                                header.Cell().Border(1).Background(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).Text("Giờ làm").Bold().AlignCenter();
                                header.Cell().Border(1).Background(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).Text("Khấu trừ").Bold().AlignCenter();
                                header.Cell().Border(1).Background(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).Text("Thực lĩnh").Bold().AlignCenter();
                            });
                            
                            int idx = 1;
                            foreach (var r in payrolls)
                            {
                                double hours = 0;
                                var staff = _db.StaffProfiles.FirstOrDefault(s => s.FullName == r.StaffName);
                                if (staff != null)
                                {
                                    hours = _db.StaffAttendances
                                        .Where(x => x.StaffId == staff.Id && x.Date.Month == currentMonth && x.Date.Year == currentYear)
                                        .Sum(x => x.WorkingHours);
                                }
                                
                                table.Cell().Border(1).Padding(5).Text(idx++.ToString()).AlignCenter();
                                table.Cell().Border(1).Padding(5).Text(r.StaffName);
                                table.Cell().Border(1).Padding(5).Text(r.BaseSalary.ToString("N0") + " đ").AlignRight();
                                table.Cell().Border(1).Padding(5).Text(hours.ToString("F1")).AlignCenter();
                                table.Cell().Border(1).Padding(5).Text(r.Deduction.ToString("N0") + " đ").AlignRight();
                                table.Cell().Border(1).Padding(5).Text(r.NetSalary.ToString("N0") + " đ").AlignRight();
                            }
                        });
                        
                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Báo cáo xuất ngày ").FontSize(9).Italic();
                            x.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(9).Italic();
                        });
                    });
                }).GeneratePdf(fileName);

                MessageBox.Show($"Đã xuất Bảng lương & Chấm công ra file PDF thành công tại:\n{fileName}", "Xuất PDF Thành Công", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Mở file PDF tự động
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(fileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Export PDF failed");
                MessageBox.Show("Đã xảy ra lỗi khi xuất file PDF: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
