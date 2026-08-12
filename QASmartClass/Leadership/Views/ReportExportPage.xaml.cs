using QASmartClass.Data;
using QASmartClass.Services;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace QASmartClass.Leadership.Views
{
    public partial class ReportExportPage : Page
    {
        private readonly AppDbContext _db;

        public ReportExportPage(AppDbContext db)
        {
            InitializeComponent();
            _db = db;
            
            // Set default dates
            DpStartDate.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DpEndDate.SelectedDate = DateTime.Now;

            // Configure QuestPDF License
            QuestPDF.Settings.License = LicenseType.Community;
        }

        private void BtnPreview_Click(object sender, RoutedEventArgs e)
        {
            string reportType = (CbReportType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Báo cáo";
            MessageBox.Show($"Bản xem trước cho: {reportType}\n(Tính năng này sẽ mở một cửa sổ Preview với dữ liệu thực tế trong tương lai.)", "Xem trước", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtStatus.Visibility = Visibility.Collapsed;
                BtnExport.IsEnabled = false;

                string typeTag = (CbReportType.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Unknown";
                bool isPdf = RbFormatPdf.IsChecked == true;

                // ======= WI-11: MOET PDF & Excel Reports =======
                if (typeTag.StartsWith("MOET_"))
                {
                    string schoolYear = "2025-2026";
                    if (DpStartDate.SelectedDate.HasValue)
                    {
                        var startD = DpStartDate.SelectedDate.Value;
                        int startYear = startD.Month >= 8 ? startD.Year : startD.Year - 1;
                        schoolYear = $"{startYear}-{startYear + 1}";
                    }
                    
                    await System.Threading.Tasks.Task.Run(() => ExportMoetReport(typeTag, schoolYear, isPdf));
                    return;
                }

                // ======= Legacy export logic =======
                DateTime startDate = DpStartDate.SelectedDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                DateTime endDate = DpEndDate.SelectedDate ?? DateTime.Now;

                string filePath = await System.Threading.Tasks.Task.Run(() => ExportLegacyReport(typeTag, isPdf, startDate, endDate));

                TxtStatus.Text = $"✅ Xuất thành công! File đã lưu tại: {filePath}";
                TxtStatus.Visibility = Visibility.Visible;
                Log.Information("Exported report {TypeTag} to {FilePath}", typeTag, filePath);

                var result = MessageBox.Show(
                    $"Đã xuất báo cáo thành công!\n\nĐường dẫn: {filePath}\n\nBạn có muốn mở file ngay?",
                    "Xuất báo cáo", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Không thể mở file báo cáo: {Error}", ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi xuất báo cáo.");
                MessageBox.Show($"Có lỗi xảy ra khi xuất file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnExport.IsEnabled = true;
            }
        }

        /// <summary>
        /// WI-11: Xuất biểu mẫu MOET dạng PDF hoặc Excel bằng QuestPDF/EPPlus.
        /// </summary>
        private void ExportMoetReport(string typeTag, string schoolYear, bool isPdf)
        {
            try
            {
                using var bgDb = new AppDbContext();
                var moetService = new MoetReportService(bgDb);
                string outputPath;

                if (isPdf)
                {
                    switch (typeTag)
                    {
                        case "MOET_20":
                            outputPath = moetService.GenerateMau20Pdf(schoolYear);
                            break;
                        case "MOET_22":
                            outputPath = moetService.GenerateMau22Pdf(schoolYear, "HK2");
                            break;
                        case "MOET_Summary":
                            outputPath = moetService.GenerateSummaryReportPdf(schoolYear);
                            break;
                        default:
                            Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show("Mẫu báo cáo không hợp lệ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                            });
                            return;
                    }
                }
                else
                {
                    switch (typeTag)
                    {
                        case "MOET_20":
                            outputPath = moetService.GenerateMau20Excel(schoolYear);
                            break;
                        case "MOET_22":
                            outputPath = moetService.GenerateMau22Excel(schoolYear, "HK2");
                            break;
                        case "MOET_Summary":
                            outputPath = moetService.GenerateSummaryReportExcel(schoolYear);
                            break;
                        default:
                            Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show("Mẫu báo cáo không hợp lệ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                            });
                            return;
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    TxtStatus.Text = $"✅ Xuất MOET thành công!\nFile: {outputPath}";
                    TxtStatus.Visibility = Visibility.Visible;

                    var result = MessageBox.Show(
                        $"Đã xuất biểu mẫu MOET thành công!\n\nĐường dẫn: {outputPath}\n\nBạn có muốn mở file ngay?",
                        "Xuất biểu mẫu MOET", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo(outputPath) { UseShellExecute = true });
                        }
                        catch (Exception ex)
                        {
                            Log.Warning("Không thể mở file: {Error}", ex.Message);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[ReportExportPage] Lỗi xuất MOET report: {TypeTag}", typeTag);
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Lỗi xuất biểu mẫu MOET:\n{ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private string ExportLegacyReport(string typeTag, bool isPdf, DateTime startDate, DateTime endDate)
        {
            try
            {
                using var bgDb = new AppDbContext();
                string ext = isPdf ? "pdf" : "xlsx";
                string fileName = $"BaoCao_{typeTag}_{DateTime.Now:yyyyMMdd_HHmmss}.{ext}";
                string outputDir = PdfTemplateHelper.GetOutputDirectory();
                string filePath = Path.Combine(outputDir, fileName);

                if (isPdf)
                {
                    GenerateLegacyPdf(bgDb, typeTag, startDate, endDate, filePath);
                }
                else
                {
                    GenerateLegacyExcel(bgDb, typeTag, startDate, endDate, filePath);
                }

                return filePath;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi sinh báo cáo legacy {TypeTag}", typeTag);
                throw;
            }
        }

        private void GenerateLegacyPdf(AppDbContext db, string typeTag, DateTime startDate, DateTime endDate, string filePath)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            string reportTitle = typeTag switch
            {
                "General" => "BÁO CÁO TỔNG HỢP (CHUYÊN MÔN & KỶ LUẬT)",
                "Emulation" => "BÁO CÁO THI ĐUA CÁC TỔ CHUYÊN MÔN",
                "TaskPerf" => "BÁO CÁO HIỆU SUẤT CÔNG VIỆC",
                "Eval360" => "BÁO CÁO KẾT QUẢ ĐÁNH GIÁ 360°",
                _ => "BÁO CÁO THỐNG KÊ"
            };

            string subtitle = $"Khoảng thời gian: Từ ngày {startDate:dd/MM/yyyy} đến ngày {endDate:dd/MM/yyyy}";

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(9));

                    page.Header().Element(c => PdfTemplateHelper.ComposeHeader(c, reportTitle, subtitle));

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        if (typeTag == "General")
                        {
                            var students = db.Students.Where(s => s.Status == "Active").ToList();
                            var totalStudents = students.Count;
                            
                            var grades = db.StudentGrades.ToList();
                            var avgGpa = grades.Any() ? Math.Round(grades.Average(g => g.Score), 2) : 0;

                            var disciplines = db.DisciplineRecords
                                .Where(d => d.Date >= startDate && d.Date <= endDate)
                                .ToList();

                            col.Item().PaddingBottom(10).Row(row =>
                            {
                                row.RelativeItem().Background(Colors.Blue.Lighten5).Padding(8).Column(c =>
                                {
                                    c.Item().Text("Tổng học sinh active").FontSize(8).FontColor(Colors.Blue.Darken3);
                                    c.Item().Text(totalStudents.ToString()).FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                                });
                                row.ConstantItem(10);
                                row.RelativeItem().Background(Colors.Green.Lighten5).Padding(8).Column(c =>
                                {
                                    c.Item().Text("Điểm trung bình (GPA)").FontSize(8).FontColor(Colors.Green.Darken3);
                                    c.Item().Text(avgGpa.ToString("0.00")).FontSize(14).Bold().FontColor(Colors.Green.Darken3);
                                });
                                row.ConstantItem(10);
                                row.RelativeItem().Background(Colors.Orange.Lighten5).Padding(8).Column(c =>
                                {
                                    c.Item().Text("Kỷ luật & Tuyên dương").FontSize(8).FontColor(Colors.Orange.Darken3);
                                    c.Item().Text(disciplines.Count.ToString()).FontSize(14).Bold().FontColor(Colors.Orange.Darken3);
                                });
                            });

                            col.Item().PaddingTop(10).Text("TOP 5 HỌC SINH CÓ GPA CAO NHẤT").Bold().FontSize(11).FontColor(Colors.Blue.Darken3);
                            col.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(30);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("STT");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).Text("Tên học sinh");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).Text("Lớp");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("GPA");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Hạnh kiểm");
                                });

                                var topStudents = students.Select(s =>
                                {
                                    var studentGrades = grades.Where(g => g.StudentId == s.Id).ToList();
                                    double gpa = studentGrades.Any() ? Math.Round(studentGrades.Average(g => g.Score), 2) : 5.0;
                                    return new { Student = s, GPA = gpa };
                                })
                                .OrderByDescending(x => x.GPA)
                                .Take(5)
                                .ToList();

                                int idx = 1;
                                foreach (var ts in topStudents)
                                {
                                    bool alt = idx % 2 == 0;
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(idx.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).Text(ts.Student.FullName);
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).Text(ts.Student.ClassName);
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(ts.GPA.ToString("0.0"));
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(ts.Student.ConductScore.ToString());
                                    idx++;
                                }
                            });

                            col.Item().PaddingTop(15).Text("DANH SÁCH KHEN THƯỞNG & KỶ LUẬT TRONG KỲ").Bold().FontSize(11).FontColor(Colors.Blue.Darken3);
                            col.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(30);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(4);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("STT");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).Text("Tên học sinh");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).Text("Lớp");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Phân loại");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).Text("Lý do");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Ngày ghi nhận");
                                });

                                int idx = 1;
                                foreach (var d in disciplines.Take(10))
                                {
                                    bool alt = idx % 2 == 0;
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(idx.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).Text(d.StudentName);
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).Text(d.ClassName);
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(d.Type);
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).Text(d.Reason);
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(d.Date.ToString("dd/MM/yyyy"));
                                    idx++;
                                }
                                if (disciplines.Count == 0)
                                {
                                    table.Cell().ColumnSpan(6).Element(c => PdfTemplateHelper.TableCellStyle(c, false)).AlignCenter().Text("Không có dữ liệu trong khoảng thời gian này.");
                                }
                            });
                        }
                        else if (typeTag == "Emulation")
                        {
                            var emulationService = new EmulationService(db);
                            var classes = emulationService.CalcClassRanking(startDate.Month, startDate.Year);
                            var teachers = emulationService.CalcTeacherEmulation(startDate.Month, startDate.Year);

                            col.Item().Text("XẾP HẠNG LỚP HỌC THI ĐUA").Bold().FontSize(11).FontColor(Colors.Blue.Darken3);
                            col.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(40);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Hạng");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).Text("Lớp");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Điểm nề nếp");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Điểm học tập");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Tổng điểm");
                                });

                                int idx = 1;
                                foreach (var c in classes.Take(10))
                                {
                                    bool alt = idx % 2 == 0;
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text($"#{c.Rank}");
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).Text(c.ClassName);
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(c.ConductScore.ToString("0.0"));
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(c.AcademicScore.ToString("0.0"));
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(c.TotalScore.ToString("0.0"));
                                    idx++;
                                }
                            });

                            col.Item().PaddingTop(15).Text("GIÁO VIÊN TIÊU BIỂU").Bold().FontSize(11).FontColor(Colors.Blue.Darken3);
                            col.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(40);
                                    columns.RelativeColumn(4);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(3);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("STT");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).Text("Họ và tên");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Đúng hạn nhiệm vụ");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Số bài giảng xong");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Điểm dự giờ");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Tổng điểm");
                                });

                                int idx = 1;
                                foreach (var t in teachers)
                                {
                                    bool alt = idx % 2 == 0;
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(idx.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).Text(t.TeacherName);
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text($"{t.OnTimeTaskRate}%");
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(t.CompletedLessons.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(t.ObservationScore.ToString("0.0"));
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(t.TotalScore.ToString("0.0"));
                                    idx++;
                                }
                            });
                        }
                        else if (typeTag == "TaskPerf")
                        {
                            var tasks = db.TaskItems
                                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                                .ToList();

                            var total = tasks.Count;
                            var done = tasks.Count(t => t.Status == "Done");
                            var inProgress = tasks.Count(t => t.Status == "InProgress");
                            var todo = tasks.Count(t => t.Status == "Todo" || t.Status == "Overdue");
                            double doneRate = total > 0 ? Math.Round((double)done / total * 100, 1) : 0;

                            col.Item().PaddingBottom(10).Row(row =>
                            {
                                row.RelativeItem().Background(Colors.Blue.Lighten5).Padding(8).Column(c =>
                                {
                                    c.Item().Text("Tổng số nhiệm vụ").FontSize(8).FontColor(Colors.Blue.Darken3);
                                    c.Item().Text(total.ToString()).FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                                });
                                row.ConstantItem(10);
                                row.RelativeItem().Background(Colors.Green.Lighten5).Padding(8).Column(c =>
                                {
                                    c.Item().Text("Đã hoàn thành").FontSize(8).FontColor(Colors.Green.Darken3);
                                    c.Item().Text($"{done} ({doneRate}%)").FontSize(14).Bold().FontColor(Colors.Green.Darken3);
                                });
                                row.ConstantItem(10);
                                row.RelativeItem().Background(Colors.Orange.Lighten5).Padding(8).Column(c =>
                                {
                                    c.Item().Text("Đang thực hiện").FontSize(8).FontColor(Colors.Orange.Darken3);
                                    c.Item().Text(inProgress.ToString()).FontSize(14).Bold().FontColor(Colors.Orange.Darken3);
                                });
                                row.ConstantItem(10);
                                row.RelativeItem().Background(Colors.Red.Lighten5).Padding(8).Column(c =>
                                {
                                    c.Item().Text("Chưa hoàn thành").FontSize(8).FontColor(Colors.Red.Darken3);
                                    c.Item().Text(todo.ToString()).FontSize(14).Bold().FontColor(Colors.Red.Darken3);
                                });
                            });

                            col.Item().PaddingTop(10).Text("CHI TIẾT DANH SÁCH NHIỆM VỤ").Bold().FontSize(11).FontColor(Colors.Blue.Darken3);
                            col.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(30);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("STT");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).Text("Tiêu đề");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).Text("Người thực hiện");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Độ ưu tiên");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Trạng thái");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Hạn chót");
                                });

                                int idx = 1;
                                foreach (var t in tasks.Take(15))
                                {
                                    bool alt = idx % 2 == 0;
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(idx.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).Text(t.Title);
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).Text(t.AssignedTo);
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(t.Priority);
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(t.Status);
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(t.Deadline.ToString("dd/MM/yyyy"));
                                    idx++;
                                }
                                if (tasks.Count == 0)
                                {
                                    table.Cell().ColumnSpan(6).Element(c => PdfTemplateHelper.TableCellStyle(c, false)).AlignCenter().Text("Không có nhiệm vụ trong khoảng thời gian này.");
                                }
                            });
                        }
                        else if (typeTag == "Eval360")
                        {
                            var evals = db.EvaluationRecords
                                .Where(e => e.EvaluatedAt >= startDate && e.EvaluatedAt <= endDate)
                                .ToList();

                            var total = evals.Count;
                            var avgRating = evals.Any() ? Math.Round(evals.Average(e => e.Rating), 1) : 0;

                            col.Item().PaddingBottom(10).Row(row =>
                            {
                                row.RelativeItem().Background(Colors.Blue.Lighten5).Padding(8).Column(c =>
                                {
                                    c.Item().Text("Tổng số lượt đánh giá").FontSize(8).FontColor(Colors.Blue.Darken3);
                                    c.Item().Text(total.ToString()).FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                                });
                                row.ConstantItem(10);
                                row.RelativeItem().Background(Colors.Green.Lighten5).Padding(8).Column(c =>
                                {
                                    c.Item().Text("Điểm đánh giá trung bình").FontSize(8).FontColor(Colors.Green.Darken3);
                                    c.Item().Text($"{avgRating} / 5.0").FontSize(14).Bold().FontColor(Colors.Green.Darken3);
                                });
                            });

                            col.Item().PaddingTop(10).Text("DANH SÁCH PHIẾU ĐÁNH GIÁ CHI TIẾT").Bold().FontSize(11).FontColor(Colors.Blue.Darken3);
                            col.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(30);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(4);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("STT");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).Text("Đối tượng đánh giá");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).Text("Người đánh giá");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).Text("Mối quan hệ");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Điểm");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).Text("Ý kiến nhận xét");
                                });

                                int idx = 1;
                                foreach (var e in evals.Take(15))
                                {
                                    bool alt = idx % 2 == 0;
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(idx.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).Text(e.TargetId);
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).Text(e.EvaluatorId);
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).Text(e.RoleRelation);
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(e.Rating.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).Text(e.Comments);
                                    idx++;
                                }
                                if (evals.Count == 0)
                                {
                                    table.Cell().ColumnSpan(6).Element(c => PdfTemplateHelper.TableCellStyle(c, false)).AlignCenter().Text("Không có phiếu đánh giá nào trong khoảng thời gian này.");
                                }
                            });
                        }

                        col.Item().Element(c => PdfTemplateHelper.ComposeSignatureBlock(c, "Người lập báo cáo", "Hiệu trưởng"));
                    });

                    page.Footer().Element(PdfTemplateHelper.ComposeFooter);
                });
            }).GeneratePdf(filePath);
        }

        private void GenerateLegacyExcel(AppDbContext db, string typeTag, DateTime startDate, DateTime endDate, string filePath)
        {
            ExcelPackage.License.SetNonCommercialOrganization("QA SmartClass");
            using var package = new ExcelPackage();

            if (typeTag == "General")
            {
                var ws = package.Workbook.Worksheets.Add("TongHop");
                
                ws.Cells[1, 1].Value = "SỞ GIÁO DỤC VÀ ĐÀO TẠO ...";
                ws.Cells[2, 1].Value = "TRƯỜNG THCS/THPT ...";
                ws.Cells[4, 1].Value = "BÁO CÁO TỔNG HỢP (CHUYÊN MÔN & KỶ LUẬT)";
                ws.Cells[4, 1].Style.Font.Size = 16;
                ws.Cells[4, 1].Style.Font.Bold = true;
                ws.Cells[5, 1].Value = $"Từ ngày: {startDate:dd/MM/yyyy} Đến ngày: {endDate:dd/MM/yyyy}";
                ws.Cells[5, 1].Style.Font.Italic = true;

                var students = db.Students.Where(s => s.Status == "Active").ToList();
                var totalStudents = students.Count;
                var grades = db.StudentGrades.ToList();
                var avgGpa = grades.Any() ? Math.Round(grades.Average(g => g.Score), 2) : 0;
                var disciplines = db.DisciplineRecords
                    .Where(d => d.Date >= startDate && d.Date <= endDate)
                    .ToList();

                ws.Cells[7, 1].Value = "Tổng học sinh active";
                ws.Cells[7, 2].Value = totalStudents;
                ws.Cells[8, 1].Value = "GPA Trung bình trường";
                ws.Cells[8, 2].Value = avgGpa;
                ws.Cells[9, 1].Value = "Tổng số sự kiện kỷ luật/khen thưởng";
                ws.Cells[9, 2].Value = disciplines.Count;
                ws.Cells[7, 1, 9, 2].Style.Font.Bold = true;

                ws.Cells[11, 1].Value = "TOP 5 HỌC SINH CÓ GPA CAO NHẤT";
                ws.Cells[11, 1].Style.Font.Bold = true;

                ws.Cells[12, 1].Value = "STT";
                ws.Cells[12, 2].Value = "Tên học sinh";
                ws.Cells[12, 3].Value = "Lớp";
                ws.Cells[12, 4].Value = "GPA";
                ws.Cells[12, 5].Value = "Hạnh kiểm";

                using (var range = ws.Cells[12, 1, 12, 5])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Navy);
                    range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                var topStudents = students.Select(s =>
                {
                    var studentGrades = grades.Where(g => g.StudentId == s.Id).ToList();
                    double gpa = studentGrades.Any() ? Math.Round(studentGrades.Average(g => g.Score), 2) : 5.0;
                    return new { Student = s, GPA = gpa };
                })
                .OrderByDescending(x => x.GPA)
                .Take(5)
                .ToList();

                int rowIdx = 13;
                int idx = 1;
                foreach (var ts in topStudents)
                {
                    ws.Cells[rowIdx, 1].Value = idx;
                    ws.Cells[rowIdx, 2].Value = ts.Student.FullName;
                    ws.Cells[rowIdx, 3].Value = ts.Student.ClassName;
                    ws.Cells[rowIdx, 4].Value = ts.GPA;
                    ws.Cells[rowIdx, 5].Value = ts.Student.ConductScore;
                    rowIdx++;
                    idx++;
                }

                ws.Cells[rowIdx + 2, 1].Value = "DANH SÁCH CHI TIẾT KHEN THƯỞNG & KỶ LUẬT TRONG KỲ";
                ws.Cells[rowIdx + 2, 1].Style.Font.Bold = true;

                int table2HeaderRow = rowIdx + 3;
                ws.Cells[table2HeaderRow, 1].Value = "STT";
                ws.Cells[table2HeaderRow, 2].Value = "Tên học sinh";
                ws.Cells[table2HeaderRow, 3].Value = "Lớp";
                ws.Cells[table2HeaderRow, 4].Value = "Phân loại";
                ws.Cells[table2HeaderRow, 5].Value = "Lý do";
                ws.Cells[table2HeaderRow, 6].Value = "Ngày ghi nhận";

                using (var range = ws.Cells[table2HeaderRow, 1, table2HeaderRow, 6])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Navy);
                    range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                int rowIdx2 = table2HeaderRow + 1;
                idx = 1;
                foreach (var d in disciplines)
                {
                    ws.Cells[rowIdx2, 1].Value = idx;
                    ws.Cells[rowIdx2, 2].Value = d.StudentName;
                    ws.Cells[rowIdx2, 3].Value = d.ClassName;
                    ws.Cells[rowIdx2, 4].Value = d.Type;
                    ws.Cells[rowIdx2, 5].Value = d.Reason;
                    ws.Cells[rowIdx2, 6].Value = d.Date.ToString("dd/MM/yyyy");
                    rowIdx2++;
                    idx++;
                }

                ws.Cells.AutoFitColumns();
            }
            else if (typeTag == "Emulation")
            {
                var emulationService = new EmulationService(db);
                var classes = emulationService.CalcClassRanking(startDate.Month, startDate.Year);
                var teachers = emulationService.CalcTeacherEmulation(startDate.Month, startDate.Year);

                var ws1 = package.Workbook.Worksheets.Add("Thi Dua Lop");
                ws1.Cells[1, 1].Value = "XẾP HẠNG THI ĐUA LỚP HỌC";
                ws1.Cells[1, 1].Style.Font.Size = 14;
                ws1.Cells[1, 1].Style.Font.Bold = true;

                ws1.Cells[3, 1].Value = "Hạng";
                ws1.Cells[3, 2].Value = "Lớp";
                ws1.Cells[3, 3].Value = "Điểm nề nếp";
                ws1.Cells[3, 4].Value = "Điểm học tập";
                ws1.Cells[3, 5].Value = "Tổng điểm";

                using (var range = ws1.Cells[3, 1, 3, 5])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Navy);
                    range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                    range.Style.Font.Bold = true;
                }

                int r = 4;
                foreach (var c in classes)
                {
                    ws1.Cells[r, 1].Value = c.Rank;
                    ws1.Cells[r, 2].Value = c.ClassName;
                    ws1.Cells[r, 3].Value = c.ConductScore;
                    ws1.Cells[r, 4].Value = c.AcademicScore;
                    ws1.Cells[r, 5].Value = c.TotalScore;
                    r++;
                }
                ws1.Cells.AutoFitColumns();

                var ws2 = package.Workbook.Worksheets.Add("Giao Vien Tieu Bieu");
                ws2.Cells[1, 1].Value = "GIÁO VIÊN TIÊU BIỂU";
                ws2.Cells[1, 1].Style.Font.Size = 14;
                ws2.Cells[1, 1].Style.Font.Bold = true;

                ws2.Cells[3, 1].Value = "STT";
                ws2.Cells[3, 2].Value = "Họ và tên";
                ws2.Cells[3, 3].Value = "Đúng hạn nhiệm vụ (%)";
                ws2.Cells[3, 4].Value = "Số bài giảng hoàn thành";
                ws2.Cells[3, 5].Value = "Điểm dự giờ";
                ws2.Cells[3, 6].Value = "Tổng điểm";

                using (var range = ws2.Cells[3, 1, 3, 6])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Navy);
                    range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                    range.Style.Font.Bold = true;
                }

                r = 4;
                int idx = 1;
                foreach (var t in teachers)
                {
                    ws2.Cells[r, 1].Value = idx;
                    ws2.Cells[r, 2].Value = t.TeacherName;
                    ws2.Cells[r, 3].Value = t.OnTimeTaskRate;
                    ws2.Cells[r, 4].Value = t.CompletedLessons;
                    ws2.Cells[r, 5].Value = t.ObservationScore;
                    ws2.Cells[r, 6].Value = t.TotalScore;
                    r++;
                    idx++;
                }
                ws2.Cells.AutoFitColumns();
            }
            else if (typeTag == "TaskPerf")
            {
                var tasks = db.TaskItems
                    .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                    .ToList();

                var ws = package.Workbook.Worksheets.Add("NhiemVu");
                ws.Cells[1, 1].Value = "DANH SÁCH CHI TIẾT HIỆU SUẤT CÔNG VIỆC";
                ws.Cells[1, 1].Style.Font.Size = 14;
                ws.Cells[1, 1].Style.Font.Bold = true;

                ws.Cells[3, 1].Value = "STT";
                ws.Cells[3, 2].Value = "Tiêu đề";
                ws.Cells[3, 3].Value = "Mô tả";
                ws.Cells[3, 4].Value = "Người thực hiện";
                ws.Cells[3, 5].Value = "Người giao";
                ws.Cells[3, 6].Value = "Phòng ban";
                ws.Cells[3, 7].Value = "Độ ưu tiên";
                ws.Cells[3, 8].Value = "Trạng thái";
                ws.Cells[3, 9].Value = "Hạn chót";

                using (var range = ws.Cells[3, 1, 3, 9])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Navy);
                    range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                    range.Style.Font.Bold = true;
                }

                int r = 4;
                int idx = 1;
                foreach (var t in tasks)
                {
                    ws.Cells[r, 1].Value = idx;
                    ws.Cells[r, 2].Value = t.Title;
                    ws.Cells[r, 3].Value = t.Description;
                    ws.Cells[r, 4].Value = t.AssignedTo;
                    ws.Cells[r, 5].Value = t.AssignedBy;
                    ws.Cells[r, 6].Value = t.Department;
                    ws.Cells[r, 7].Value = t.Priority;
                    ws.Cells[r, 8].Value = t.Status;
                    ws.Cells[r, 9].Value = t.Deadline.ToString("dd/MM/yyyy");
                    r++;
                    idx++;
                }
                ws.Cells.AutoFitColumns();
            }
            else if (typeTag == "Eval360")
            {
                var evals = db.EvaluationRecords
                    .Where(e => e.EvaluatedAt >= startDate && e.EvaluatedAt <= endDate)
                    .ToList();

                var ws = package.Workbook.Worksheets.Add("DanhGia360");
                ws.Cells[1, 1].Value = "DANH SÁCH PHIẾU ĐÁNH GIÁ 360 ĐỘ CHI TIẾT";
                ws.Cells[1, 1].Style.Font.Size = 14;
                ws.Cells[1, 1].Style.Font.Bold = true;

                ws.Cells[3, 1].Value = "STT";
                ws.Cells[3, 2].Value = "Đối tượng đánh giá";
                ws.Cells[3, 3].Value = "Người đánh giá";
                ws.Cells[3, 4].Value = "Mối quan hệ";
                ws.Cells[3, 5].Value = "Điểm";
                ws.Cells[3, 6].Value = "Ý kiến nhận xét";
                ws.Cells[3, 7].Value = "Thời gian đánh giá";

                using (var range = ws.Cells[3, 1, 3, 7])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Navy);
                    range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                    range.Style.Font.Bold = true;
                }

                int r = 4;
                int idx = 1;
                foreach (var e in evals)
                {
                    ws.Cells[r, 1].Value = idx;
                    ws.Cells[r, 2].Value = e.TargetId;
                    ws.Cells[r, 3].Value = e.EvaluatorId;
                    ws.Cells[r, 4].Value = e.RoleRelation;
                    ws.Cells[r, 5].Value = e.Rating;
                    ws.Cells[r, 6].Value = e.Comments;
                    ws.Cells[r, 7].Value = e.EvaluatedAt.ToString("dd/MM/yyyy HH:mm");
                    r++;
                    idx++;
                }
                ws.Cells.AutoFitColumns();
            }

            package.SaveAs(new FileInfo(filePath));
        }
    }
}

