using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QASmartClass.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Serilog;

namespace QASmartClass.Services
{
    /// <summary>
    /// WI-11: Biểu mẫu MOET — Xuất PDF theo chuẩn Bộ GD&ĐT.
    /// Mẫu 20-THCS: Thống kê sĩ số theo giới tính, dân tộc, khối lớp.
    /// Mẫu 22-THCS: Xếp loại học lực + hạnh kiểm theo khối.
    /// Mẫu Tổng kết: Bảng GV (trình độ) + HS (sĩ số, tỷ lệ lên lớp).
    /// </summary>
    public class MoetReportService
    {
        private readonly AppDbContext _db;

        public MoetReportService(AppDbContext db)
        {
            _db = db;
        }

        private string GetSchoolYearSetting(string defaultYear)
        {
            try
            {
                var setting = _db.SystemSettings.FirstOrDefault(s => s.Id == "SchoolYear");
                if (setting != null && !string.IsNullOrWhiteSpace(setting.Value))
                {
                    return setting.Value;
                }
            }
            catch { }
            return defaultYear;
        }

        // ════════════════════════════════════════════════════════════════
        //  MẪU 20-THCS: THỐNG KÊ SĨ SỐ THEO GIỚI TÍNH, DÂN TỘC
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Xuất Mẫu 20-THCS: Thống kê sĩ số theo khối lớp, giới tính, dân tộc.
        /// </summary>
        public string GenerateMau20Pdf(string schoolYear = "2025-2026")
        {
            schoolYear = GetSchoolYearSetting(schoolYear);
            try
            {
                var activeRosterIds = _db.ClassRosters
                    .Where(r => r.SchoolYear == schoolYear && r.IsActive)
                    .Select(r => r.Id)
                    .ToList();

                var studentIds = _db.ClassRosterStudents
                    .Where(crs => activeRosterIds.Contains(crs.RosterId))
                    .Select(crs => crs.StudentId)
                    .Distinct()
                    .ToList();

                var students = _db.Students
                    .Where(s => s.Status == "Active" && studentIds.Contains(s.Id))
                    .ToList();

                var gradeLevels = students.Select(s => ExtractGradeLevel(s.ClassName)).Distinct().OrderBy(g => g).ToList();

                string outputPath = PdfTemplateHelper.GetOutputPath("MOET_Mau20_THCS", schoolYear);

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(1.5f, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(9));

                        page.Header().Element(c => ComposeMoetHeader(c,
                            "BIỂU MẪU 20-THCS",
                            "THỐNG KÊ SĨ SỐ HỌC SINH ĐẦU NĂM HỌC",
                            $"Năm học: {schoolYear}"));

                        page.Content().PaddingTop(10).Column(col =>
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(30);  // STT
                                    columns.RelativeColumn(2);   // Khối lớp
                                    columns.RelativeColumn();    // Tổng số lớp
                                    columns.RelativeColumn();    // Tổng HS
                                    columns.RelativeColumn();    // Nữ
                                    columns.RelativeColumn();    // Dân tộc thiểu số
                                    columns.RelativeColumn();    // Nữ DTTS
                                    columns.RelativeColumn();    // Tỷ lệ nữ %
                                });

                                // Header
                                table.Header(header =>
                                {
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("STT");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).Text("Khối lớp");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Số lớp");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Tổng HS");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Nữ");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("DTTS");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Nữ DTTS");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Tỷ lệ nữ (%)");
                                });

                                int stt = 1;
                                int grandTotal = 0, grandFemale = 0, grandMinority = 0, grandFemaleMinority = 0;

                                foreach (var grade in gradeLevels)
                                {
                                    var gradeStudents = students.Where(s => ExtractGradeLevel(s.ClassName) == grade).ToList();
                                    int total = gradeStudents.Count;
                                    int female = gradeStudents.Count(s => s.Gender == "Nữ");
                                    int minority = gradeStudents.Count(s => !string.IsNullOrWhiteSpace(s.Ethnicity) && !s.Ethnicity.Trim().Equals("Kinh", StringComparison.OrdinalIgnoreCase));
                                    int femaleMinority = gradeStudents.Count(s => s.Gender == "Nữ" && !string.IsNullOrWhiteSpace(s.Ethnicity) && !s.Ethnicity.Trim().Equals("Kinh", StringComparison.OrdinalIgnoreCase));
                                    int classCount = gradeStudents.Select(s => s.ClassName).Distinct().Count();
                                    double femaleRatio = total > 0 ? Math.Round((double)female / total * 100, 1) : 0;

                                    grandTotal += total;
                                    grandFemale += female;
                                    grandMinority += minority;
                                    grandFemaleMinority += femaleMinority;

                                    bool alt = stt % 2 == 0;
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(stt.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).Text($"Khối {grade}");
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(classCount.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(total.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(female.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(minority.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(femaleMinority.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text($"{femaleRatio}%");
                                    stt++;
                                }

                                // Tổng cộng
                                double grandFemaleRatio = grandTotal > 0 ? Math.Round((double)grandFemale / grandTotal * 100, 1) : 0;
                                table.Cell().Element(c => c.Background(Colors.Grey.Lighten3).Padding(4).DefaultTextStyle(t => t.FontSize(9).Bold())).AlignCenter().Text("");
                                table.Cell().Element(c => c.Background(Colors.Grey.Lighten3).Padding(4).DefaultTextStyle(t => t.FontSize(9).Bold())).Text("TỔNG CỘNG");
                                table.Cell().Element(c => c.Background(Colors.Grey.Lighten3).Padding(4).DefaultTextStyle(t => t.FontSize(9).Bold())).AlignCenter().Text("");
                                table.Cell().Element(c => c.Background(Colors.Grey.Lighten3).Padding(4).DefaultTextStyle(t => t.FontSize(9).Bold())).AlignCenter().Text(grandTotal.ToString());
                                table.Cell().Element(c => c.Background(Colors.Grey.Lighten3).Padding(4).DefaultTextStyle(t => t.FontSize(9).Bold())).AlignCenter().Text(grandFemale.ToString());
                                table.Cell().Element(c => c.Background(Colors.Grey.Lighten3).Padding(4).DefaultTextStyle(t => t.FontSize(9).Bold())).AlignCenter().Text(grandMinority.ToString());
                                table.Cell().Element(c => c.Background(Colors.Grey.Lighten3).Padding(4).DefaultTextStyle(t => t.FontSize(9).Bold())).AlignCenter().Text(grandFemaleMinority.ToString());
                                table.Cell().Element(c => c.Background(Colors.Grey.Lighten3).Padding(4).DefaultTextStyle(t => t.FontSize(9).Bold())).AlignCenter().Text($"{grandFemaleRatio}%");
                            });

                            col.Item().Element(c => ComposeSignatureBlockWithSignature(c, "Người lập biểu", "Hiệu trưởng"));
                        });

                        page.Footer().Element(PdfTemplateHelper.ComposeFooter);
                    });
                }).GeneratePdf(outputPath);

                Log.Information("[MOET] Đã xuất Mẫu 20-THCS: {Path}", outputPath);
                return outputPath;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[MOET] Lỗi xuất Mẫu 20-THCS");
                throw;
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  MẪU 22-THCS: XẾP LOẠI HỌC LỰC + HẠNH KIỂM
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Xuất Mẫu 22-THCS: Xếp loại học lực và hạnh kiểm theo khối.
        /// </summary>
        public string GenerateMau22Pdf(string schoolYear = "2025-2026", string semester = "HK2")
        {
            schoolYear = GetSchoolYearSetting(schoolYear);
            try
            {
                var activeRosterIds = _db.ClassRosters
                    .Where(r => r.SchoolYear == schoolYear && r.Semester == semester && r.IsActive)
                    .Select(r => r.Id)
                    .ToList();

                var studentIds = _db.ClassRosterStudents
                    .Where(crs => activeRosterIds.Contains(crs.RosterId))
                    .Select(crs => crs.StudentId)
                    .Distinct()
                    .ToList();

                var students = _db.Students
                    .Where(s => s.Status == "Active" && studentIds.Contains(s.Id))
                    .ToList();

                var gradeLevels = students.Select(s => ExtractGradeLevel(s.ClassName)).Distinct().OrderBy(g => g).ToList();
                var gradingService = new TT22GradingService(_db);

                string outputPath = PdfTemplateHelper.GetOutputPath("MOET_Mau22_THCS", schoolYear);

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(1.5f, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(8));

                        page.Header().Element(c => ComposeMoetHeader(c,
                            "BIỂU MẪU 22-THCS",
                            "KẾT QUẢ XẾP LOẠI HỌC LỰC VÀ HẠNH KIỂM",
                            $"Năm học: {schoolYear} | Học kỳ: {semester}"));

                        page.Content().PaddingTop(10).Column(col =>
                        {
                            // ===== A. XẾP LOẠI HỌC LỰC =====
                            col.Item().Text("A. XẾP LOẠI HỌC LỰC").Bold().FontSize(10).FontColor(Colors.Blue.Darken3);
                            col.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(25);  // STT
                                    columns.RelativeColumn(2);   // Khối
                                    columns.RelativeColumn();    // Tổng HS
                                    columns.RelativeColumn();    // Tốt
                                    columns.RelativeColumn();    // Khá
                                    columns.RelativeColumn();    // Đạt
                                    columns.RelativeColumn();    // Chưa đạt
                                    columns.RelativeColumn();    // % Tốt+Khá
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("#");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).Text("Khối");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Tổng");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Tốt");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Khá");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Đạt");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Chưa đạt");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("% T+K");
                                });

                                int stt = 1;
                                foreach (var grade in gradeLevels)
                                {
                                    var gradeStudents = students.Where(s => ExtractGradeLevel(s.ClassName) == grade).ToList();
                                    var stats = CalculateAcademicStats(gradeStudents, gradingService, schoolYear, semester);
                                    bool alt = stt % 2 == 0;

                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(stt.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).Text($"Khối {grade}");
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(stats.Total.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(stats.Tot.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(stats.Kha.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(stats.Dat.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(stats.ChuaDat.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text($"{stats.TotKhaPercent:0.0}%");
                                    stt++;
                                }
                            });

                            // ===== B. XẾP LOẠI HẠNH KIỂM =====
                            col.Item().PaddingTop(15).Text("B. XẾP LOẠI HẠNH KIỂM").Bold().FontSize(10).FontColor(Colors.Blue.Darken3);
                            col.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(25);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("#");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).Text("Khối");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Tổng");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Tốt");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Khá");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Đạt");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Chưa đạt");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("% Tốt+Khá");
                                });

                                int stt = 1;
                                foreach (var grade in gradeLevels)
                                {
                                    var gradeStudents = students.Where(s => ExtractGradeLevel(s.ClassName) == grade).ToList();
                                    int total = gradeStudents.Count;
                                    int tot = gradeStudents.Count(s => s.ConductScore >= 80);
                                    int kha = gradeStudents.Count(s => s.ConductScore >= 65 && s.ConductScore < 80);
                                    int dat = gradeStudents.Count(s => s.ConductScore >= 50 && s.ConductScore < 65);
                                    int chuadat = gradeStudents.Count(s => s.ConductScore < 50);
                                    double tkPercent = total > 0 ? Math.Round((double)(tot + kha) / total * 100, 1) : 0;

                                    bool alt = stt % 2 == 0;
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(stt.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).Text($"Khối {grade}");
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(total.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(tot.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(kha.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(dat.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(chuadat.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text($"{tkPercent}%");
                                    stt++;
                                }
                            });

                            col.Item().Element(c => ComposeSignatureBlockWithSignature(c, "Người lập biểu", "Hiệu trưởng"));
                        });

                        page.Footer().Element(PdfTemplateHelper.ComposeFooter);
                    });
                }).GeneratePdf(outputPath);

                Log.Information("[MOET] Đã xuất Mẫu 22-THCS: {Path}", outputPath);
                return outputPath;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[MOET] Lỗi xuất Mẫu 22-THCS");
                throw;
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  MẪU TỔNG KẾT: BẢNG GV + HS
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Xuất Mẫu Tổng kết: Bảng GV (trình độ) + HS (sĩ số, tỷ lệ lên lớp).
        /// </summary>
        public string GenerateSummaryReportPdf(string schoolYear = "2025-2026")
        {
            schoolYear = GetSchoolYearSetting(schoolYear);
            try
            {
                var activeRosterIds = _db.ClassRosters
                    .Where(r => r.SchoolYear == schoolYear && r.IsActive)
                    .Select(r => r.Id)
                    .ToList();

                var studentIds = _db.ClassRosterStudents
                    .Where(crs => activeRosterIds.Contains(crs.RosterId))
                    .Select(crs => crs.StudentId)
                    .Distinct()
                    .ToList();

                var students = _db.Students
                    .Where(s => s.Status == "Active" && studentIds.Contains(s.Id))
                    .ToList();

                var teachers = _db.TeacherProfiles.Where(t => t.IsActive).ToList();

                string outputPath = PdfTemplateHelper.GetOutputPath("MOET_TongKet", schoolYear);

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(9));

                        page.Header().Element(c => ComposeMoetHeader(c,
                            "BÁO CÁO TỔNG KẾT",
                            "TÌNH HÌNH ĐỘI NGŨ GIÁO VIÊN VÀ HỌC SINH",
                            $"Năm học: {schoolYear}"));

                        page.Content().PaddingTop(10).Column(col =>
                        {
                            // ===== I. TÌNH HÌNH ĐỘI NGŨ =====
                            col.Item().Text("I. TÌNH HÌNH ĐỘI NGŨ GIÁO VIÊN").Bold().FontSize(11).FontColor(Colors.Blue.Darken3);

                            // KPI cards row
                            col.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                                {
                                    c.Item().Text("Tổng số CBGV").FontSize(9).FontColor(Colors.Grey.Medium);
                                    c.Item().Text(teachers.Count.ToString()).FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                                });
                                row.ConstantItem(10);
                                int thacSi = teachers.Count(t => t.Title == "ThS" || t.Title == "Thạc sĩ");
                                row.RelativeItem().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                                {
                                    c.Item().Text("Thạc sĩ trở lên").FontSize(9).FontColor(Colors.Grey.Medium);
                                    c.Item().Text(thacSi.ToString()).FontSize(20).Bold().FontColor(Colors.Green.Darken2);
                                });
                                row.ConstantItem(10);
                                double thacSiPercent = teachers.Count > 0 ? Math.Round((double)thacSi / teachers.Count * 100, 1) : 0;
                                row.RelativeItem().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                                {
                                    c.Item().Text("Tỷ lệ trên chuẩn").FontSize(9).FontColor(Colors.Grey.Medium);
                                    c.Item().Text($"{thacSiPercent}%").FontSize(20).Bold().FontColor(Colors.Orange.Darken2);
                                });
                            });

                            // Teacher qualification table
                            col.Item().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(30);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("STT");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).Text("Trình độ");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Số lượng");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Tỷ lệ (%)");
                                });

                                var titleGroups = new[] { "GV", "ThS", "TS", "PGS", "GS" };
                                var titleLabels = new Dictionary<string, string>
                                {
                                    { "GV", "Cử nhân / Đại học" },
                                    { "ThS", "Thạc sĩ" },
                                    { "TS", "Tiến sĩ" },
                                    { "PGS", "Phó Giáo sư" },
                                    { "GS", "Giáo sư" }
                                };

                                int stt = 1;
                                foreach (var title in titleGroups)
                                {
                                    int count = teachers.Count(t => t.Title == title);
                                    if (count == 0 && title != "GV" && title != "ThS") continue;
                                    double pct = teachers.Count > 0 ? Math.Round((double)count / teachers.Count * 100, 1) : 0;
                                    bool alt = stt % 2 == 0;

                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(stt.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).Text(titleLabels.GetValueOrDefault(title, title));
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(count.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text($"{pct}%");
                                    stt++;
                                }
                            });

                            // ===== II. TÌNH HÌNH HỌC SINH =====
                            col.Item().PaddingTop(20).Text("II. TÌNH HÌNH HỌC SINH").Bold().FontSize(11).FontColor(Colors.Blue.Darken3);

                            var gradeLevels = students.Select(s => ExtractGradeLevel(s.ClassName)).Distinct().OrderBy(g => g).ToList();

                            col.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(30);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("STT");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).Text("Khối lớp");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Số lớp");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Sĩ số");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Nữ");
                                    header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("DTTS");
                                });

                                int stt = 1;
                                int totalStudents = 0, totalFemale = 0, totalMinority = 0;
                                foreach (var grade in gradeLevels)
                                {
                                    var gs = students.Where(s => ExtractGradeLevel(s.ClassName) == grade).ToList();
                                    int classCount = gs.Select(s => s.ClassName).Distinct().Count();
                                    int female = gs.Count(s => s.Gender == "Nữ");
                                    int minority = gs.Count(s => !string.IsNullOrWhiteSpace(s.Ethnicity) && !s.Ethnicity.Trim().Equals("Kinh", StringComparison.OrdinalIgnoreCase));

                                    totalStudents += gs.Count;
                                    totalFemale += female;
                                    totalMinority += minority;

                                    bool alt = stt % 2 == 0;
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(stt.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).Text($"Khối {grade}");
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(classCount.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(gs.Count.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(female.ToString());
                                    table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, alt)).AlignCenter().Text(minority.ToString());
                                    stt++;
                                }

                                // Tổng cộng
                                table.Cell().Element(c => c.Background(Colors.Grey.Lighten3).Padding(4).DefaultTextStyle(t => t.FontSize(9).Bold())).AlignCenter().Text("");
                                table.Cell().Element(c => c.Background(Colors.Grey.Lighten3).Padding(4).DefaultTextStyle(t => t.FontSize(9).Bold())).Text("TỔNG CỘNG");
                                table.Cell().Element(c => c.Background(Colors.Grey.Lighten3).Padding(4).DefaultTextStyle(t => t.FontSize(9).Bold())).AlignCenter().Text("");
                                table.Cell().Element(c => c.Background(Colors.Grey.Lighten3).Padding(4).DefaultTextStyle(t => t.FontSize(9).Bold())).AlignCenter().Text(totalStudents.ToString());
                                table.Cell().Element(c => c.Background(Colors.Grey.Lighten3).Padding(4).DefaultTextStyle(t => t.FontSize(9).Bold())).AlignCenter().Text(totalFemale.ToString());
                                table.Cell().Element(c => c.Background(Colors.Grey.Lighten3).Padding(4).DefaultTextStyle(t => t.FontSize(9).Bold())).AlignCenter().Text(totalMinority.ToString());
                            });

                            col.Item().Element(c => ComposeSignatureBlockWithSignature(c, "Người lập biểu", "Hiệu trưởng"));
                        });

                        page.Footer().Element(PdfTemplateHelper.ComposeFooter);
                    });
                }).GeneratePdf(outputPath);

                Log.Information("[MOET] Đã xuất Báo cáo Tổng kết: {Path}", outputPath);
                return outputPath;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[MOET] Lỗi xuất Báo cáo Tổng kết");
                throw;
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  BACKWARD COMPATIBLE: giữ nguyên CSV methods (deprecated)
        // ════════════════════════════════════════════════════════════════

        [Obsolete("Use GenerateMau20Pdf() instead")]
        public string GenerateStudentCsvReport()
        {
            var students = _db.Students.ToList();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("MaHS,HoTen,Lop,NgaySinh,GioiTinh,TrangThai");
            foreach (var s in students)
                sb.AppendLine($"{s.Id},{s.FullName},{s.ClassName},2010-01-01,{s.Gender},{s.Status}");
            return sb.ToString();
        }

        [Obsolete("Use GenerateMau22Pdf() instead")]
        public string GenerateAttendanceCsvReport()
        {
            var records = _db.AttendanceRecords.OrderByDescending(a => a.Date).Take(100).ToList();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("MaHS,HoTen,Ngay,TrangThai,GhiChu");
            foreach (var r in records)
            {
                var student = _db.Students.Find(r.StudentId);
                string studentName = student?.FullName ?? "N/A";
                sb.AppendLine($"{r.StudentId},{studentName},{r.Date:yyyy-MM-dd},{r.Status},{r.Note}");
            }
            return sb.ToString();
        }

        [Obsolete("Use GenerateSummaryReportPdf() instead")]
        public string GenerateScoreCsvReport()
        {
            var grades = _db.StudentGrades.Take(200).ToList();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("MaHS,HoTen,LoaiDiem,DiemSo,GhiChu");
            foreach (var g in grades)
            {
                var student = _db.Students.Find(g.StudentId);
                string studentName = student?.FullName ?? "N/A";
                sb.AppendLine($"{g.StudentId},{studentName},{g.GradeTypeId},{g.Score},{g.Notes}");
            }
            return sb.ToString();
        }

        // ════════════════════════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// MOET header: Bộ GD&ĐT heading + mã biểu + tiêu đề.
        /// </summary>
        private void ComposeMoetHeader(IContainer container, string formCode, string title, string subtitle)
        {
            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem(4).Column(left =>
                    {
                        left.Item().AlignCenter().Text("BỘ GIÁO DỤC VÀ ĐÀO TẠO").FontSize(9).Italic();
                        left.Item().AlignCenter().Text("TRƯỜNG THCS/THPT ...").FontSize(10).Bold();
                    });

                    row.RelativeItem(5).Column(right =>
                    {
                        right.Item().AlignCenter().Text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM").FontSize(10).Bold();
                        right.Item().AlignCenter().Text("Độc lập — Tự do — Hạnh phúc").FontSize(10).Italic().Underline();
                    });
                });

                col.Item().PaddingTop(5).AlignRight().Text(text =>
                {
                    text.Span("Mã biểu: ").FontSize(8).Italic();
                    text.Span(formCode).FontSize(8).Bold();
                });

                col.Item().PaddingTop(10).AlignCenter().Text(title).FontSize(14).Bold();

                if (!string.IsNullOrEmpty(subtitle))
                {
                    col.Item().AlignCenter().Text(subtitle).FontSize(10).Italic();
                }

                col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);
            });
        }

        /// <summary>
        /// Extract grade level number from class name (e.g., "10A1" -> "10", "6A" -> "6").
        /// </summary>
        private static string ExtractGradeLevel(string className)
        {
            if (string.IsNullOrEmpty(className)) return "0";
            var digits = new string(className.TakeWhile(char.IsDigit).ToArray());
            return string.IsNullOrEmpty(digits) ? "0" : digits;
        }

        /// <summary>
        /// Calculate academic classification stats for a group of students.
        /// </summary>
        private void ComposeSignatureBlockWithSignature(IContainer container, string leftTitle = "Người lập biểu", string rightTitle = "Hiệu trưởng")
        {
            string location = "...";
            try
            {
                var citySetting = _db.SystemSettings.FirstOrDefault(s => s.Id == "SchoolCity" || s.Id == "General_SchoolCity" || s.Id == "SchoolLocation");
                if (citySetting != null && !string.IsNullOrWhiteSpace(citySetting.Value))
                {
                    location = citySetting.Value;
                }
            }
            catch { }

            container.PaddingTop(20).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().AlignCenter().Text(leftTitle).FontSize(10).Bold();
                    col.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").FontSize(8).Italic();
                    col.Item().Height(60);
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().AlignCenter().Text($"{location}, ngày {DateTime.Now:dd} tháng {DateTime.Now:MM} năm {DateTime.Now:yyyy}")
                        .FontSize(9).Italic();
                    col.Item().AlignCenter().Text(rightTitle).FontSize(10).Bold();
                    col.Item().AlignCenter().Text("(Ký, đóng dấu)").FontSize(8).Italic();

                    string encSigPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images", "hieutruong_signature.enc");
                    if (System.IO.File.Exists(encSigPath))
                    {
                        try
                        {
                            byte[] encBytes = System.IO.File.ReadAllBytes(encSigPath);
                            byte[] decBytes = AwardService.DecryptBytes(encBytes);
                            col.Item().AlignCenter().Height(50).Image(decBytes);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "MoetReportService: Failed to decrypt signature image");
                            col.Item().Height(50);
                        }
                    }
                    else
                    {
                        col.Item().Height(50);
                    }
                });
            });
        }

        private AcademicStatsDto CalculateAcademicStats(List<Student> students, TT22GradingService gradingService, string schoolYear, string semester)
        {
            var dto = new AcademicStatsDto();
            var studentIds = students.Select(s => s.Id).ToList();

            var gkTypeIds = _db.GradeTypeMasters
                .Where(t => t.Id == 2 || t.ShortName == "GK" || t.Code == "GK" || t.Code == "KT1Tiet")
                .Select(t => t.Id)
                .ToList();
            var ckTypeIds = _db.GradeTypeMasters
                .Where(t => t.Id == 3 || t.ShortName == "CK" || t.Code == "CK" || t.Code == "HocKy")
                .Select(t => t.Id)
                .ToList();
            var typeWeights = _db.GradeTypeMasters.ToDictionary(t => t.Id, t => t.Weight);

            // Load all rosters students in memory
            var allRosterStudents = _db.ClassRosterStudents
                .Where(crs => studentIds.Contains(crs.StudentId))
                .ToList();

            var studentRosterIdsMap = allRosterStudents
                .GroupBy(crs => crs.StudentId)
                .ToDictionary(g => g.Key, g => g.Select(crs => crs.RosterId).ToList());

            var uniqueRosterIds = allRosterStudents.Select(crs => crs.RosterId).Distinct().ToList();

            var rosters = _db.ClassRosters
                .Where(r => uniqueRosterIds.Contains(r.Id) && r.SchoolYear == schoolYear && r.Semester == semester && r.IsActive)
                .ToList();

            var activeRosterIds = rosters.Select(r => r.Id).ToHashSet();
            var activeRostersMap = rosters.ToDictionary(r => r.Id, r => r);

            // Load all grades in memory
            var allGrades = _db.StudentGrades
                .Where(g => studentIds.Contains(g.StudentId) && activeRosterIds.Contains(g.RosterId))
                .ToList();

            var gradesMap = allGrades
                .GroupBy(g => (g.StudentId, g.RosterId))
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var student in students)
            {
                if (!studentRosterIdsMap.TryGetValue(student.Id, out var studentRosterIds))
                {
                    continue; // Skip student not enrolled
                }

                var studentActiveRosters = studentRosterIds
                    .Where(id => activeRosterIds.Contains(id))
                    .Select(id => activeRostersMap[id])
                    .ToList();

                if (!studentActiveRosters.Any())
                {
                    continue; // Skip student not enrolled in this semester
                }

                dto.Total++;

                bool isCDD = false;
                var subjectAvgs = new List<double>();
                foreach (var roster in studentActiveRosters)
                {
                    var result = CalculateSubjectAverageFromMemory(student.Id, roster.Id, gradesMap, gkTypeIds, ckTypeIds, typeWeights);
                    if (result.HasData)
                    {
                        if (result.Classification != null && result.Classification.StartsWith("Chưa đủ điểm"))
                        {
                            isCDD = true;
                        }
                        else
                        {
                            subjectAvgs.Add(result.Average);
                        }
                    }
                }

                string classification = "Chưa đạt";
                if (isCDD || !subjectAvgs.Any())
                {
                    classification = "Chưa đạt";
                }
                else
                {
                    int n = subjectAvgs.Count;
                    int n8 = subjectAvgs.Count(a => a >= 8.0);
                    int n65 = subjectAvgs.Count(a => a >= 6.5);
                    int n50 = subjectAvgs.Count(a => a >= 5.0);
                    int n35 = subjectAvgs.Count(a => a >= 3.5);

                    // Quy tắc xếp loại theo TT22:
                    // - Tốt: Tất cả các môn >= 6.5 và có ít nhất 6 môn >= 8.0 (hoặc tất cả nếu n < 6)
                    // - Khá: Tất cả các môn >= 5.0 và có ít nhất 6 môn >= 6.5 (hoặc tất cả nếu n < 6)
                    // - Đạt: Nhiều nhất 1 môn < 5.0 nhưng phải >= 3.5
                    // - Chưa đạt: các trường hợp còn lại
                    if (n65 == n && (n8 >= 6 || (n < 6 && n8 == n)))
                    {
                        classification = "Tốt";
                    }
                    else if (n50 == n && (n65 >= 6 || (n < 6 && n65 == n)))
                    {
                        classification = "Khá";
                    }
                    else if (n35 == n && n50 >= n - 1)
                    {
                        classification = "Đạt";
                    }
                    else
                    {
                        classification = "Chưa đạt";
                    }
                }

                switch (classification)
                {
                    case "Tốt": dto.Tot++; break;
                    case "Khá": dto.Kha++; break;
                    case "Đạt": dto.Dat++; break;
                    default: dto.ChuaDat++; break;
                }
            }

            return dto;
        }

        private static SubjectAverageResult CalculateSubjectAverageFromMemory(int studentId, int rosterId, Dictionary<(int, int), List<StudentGrade>> gradesMap, List<int> gkTypeIds, List<int> ckTypeIds, Dictionary<int, int> typeWeights)
        {
            if (!gradesMap.TryGetValue((studentId, rosterId), out var grades) || !grades.Any())
                return new SubjectAverageResult { Average = 0, Classification = "Chưa có điểm", HasData = false };

            bool hasGK = grades.Any(g => gkTypeIds.Contains(g.GradeTypeId));
            bool hasCK = grades.Any(g => ckTypeIds.Contains(g.GradeTypeId));

            if (!hasGK || !hasCK)
            {
                string classification = "Chưa đủ điểm";
                if (!hasGK && !hasCK) classification = "Chưa đủ điểm (Thiếu GK, CK)";
                else if (!hasGK) classification = "Chưa đủ điểm (Thiếu GK)";
                else if (!hasCK) classification = "Chưa đủ điểm (Thiếu CK)";

                return new SubjectAverageResult
                {
                    Average = 0,
                    Classification = classification,
                    HasData = true,
                    TotalGrades = grades.Count
                };
            }

            double totalWeighted = 0;
            int totalWeight = 0;

            foreach (var g in grades)
            {
                if (g.Notes == "Miễn") continue;
                int weight = typeWeights.TryGetValue(g.GradeTypeId, out var w) ? w : 1;
                totalWeighted += g.Score * weight;
                totalWeight += weight;
            }

            double avg = totalWeight > 0 ? Math.Round(totalWeighted / totalWeight, 2) : 0;

            return new SubjectAverageResult
            {
                Average = avg,
                Classification = TT22GradingService.ClassifyAcademic(avg),
                HasData = true,
                TotalGrades = grades.Count
            };
        }

        private class AcademicStatsDto
        {
            public int Total { get; set; }
            public int Tot { get; set; }
            public int Kha { get; set; }
            public int Dat { get; set; }
            public int ChuaDat { get; set; }
            public double TotKhaPercent => Total > 0 ? Math.Round((double)(Tot + Kha) / Total * 100, 1) : 0;
        }

        public string GenerateMau20Excel(string schoolYear = "2025-2026")
        {
            schoolYear = GetSchoolYearSetting(schoolYear);
            try
            {
                var activeRosterIds = _db.ClassRosters
                    .Where(r => r.SchoolYear == schoolYear && r.IsActive)
                    .Select(r => r.Id)
                    .ToList();

                var studentIds = _db.ClassRosterStudents
                    .Where(crs => activeRosterIds.Contains(crs.RosterId))
                    .Select(crs => crs.StudentId)
                    .Distinct()
                    .ToList();

                var students = _db.Students
                    .Where(s => s.Status == "Active" && studentIds.Contains(s.Id))
                    .ToList();

                var gradeLevels = students.Select(s => ExtractGradeLevel(s.ClassName)).Distinct().OrderBy(g => g).ToList();

                string outputPath = Path.Combine(PdfTemplateHelper.GetOutputDirectory(), $"MOET_Mau20_THCS_{schoolYear}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

                ExcelPackage.License.SetNonCommercialOrganization("QA SmartClass");
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Mẫu 20-THCS");

                // Header
                worksheet.Cells[1, 1].Value = "BIỂU MẪU 20-THCS";
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[2, 1].Value = "THỐNG KÊ SĨ SỐ HỌC SINH ĐẦU NĂM HỌC";
                worksheet.Cells[2, 1].Style.Font.Bold = true;
                worksheet.Cells[2, 1].Style.Font.Size = 14;
                worksheet.Cells[3, 1].Value = $"Năm học: {schoolYear}";
                worksheet.Cells[3, 1].Style.Font.Italic = true;

                int startRow = 5;
                worksheet.Cells[startRow, 1].Value = "STT";
                worksheet.Cells[startRow, 2].Value = "Khối lớp";
                worksheet.Cells[startRow, 3].Value = "Số lớp";
                worksheet.Cells[startRow, 4].Value = "Tổng HS";
                worksheet.Cells[startRow, 5].Value = "Nữ";
                worksheet.Cells[startRow, 6].Value = "DTTS";
                worksheet.Cells[startRow, 7].Value = "Nữ DTTS";
                worksheet.Cells[startRow, 8].Value = "Tỷ lệ nữ (%)";

                using (var r = worksheet.Cells[startRow, 1, startRow, 8])
                {
                    r.Style.Font.Bold = true;
                    r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    r.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    r.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                int stt = 1;
                int grandTotal = 0, grandFemale = 0, grandMinority = 0, grandFemaleMinority = 0;

                foreach (var grade in gradeLevels)
                {
                    var gradeStudents = students.Where(s => ExtractGradeLevel(s.ClassName) == grade).ToList();
                    int total = gradeStudents.Count;
                    int female = gradeStudents.Count(s => s.Gender == "Nữ");
                    int minority = gradeStudents.Count(s => !string.IsNullOrWhiteSpace(s.Ethnicity) && !s.Ethnicity.Trim().Equals("Kinh", StringComparison.OrdinalIgnoreCase));
                    int femaleMinority = gradeStudents.Count(s => s.Gender == "Nữ" && !string.IsNullOrWhiteSpace(s.Ethnicity) && !s.Ethnicity.Trim().Equals("Kinh", StringComparison.OrdinalIgnoreCase));
                    int classCount = gradeStudents.Select(s => s.ClassName).Distinct().Count();
                    double femaleRatio = total > 0 ? Math.Round((double)female / total * 100, 1) : 0;

                    grandTotal += total;
                    grandFemale += female;
                    grandMinority += minority;
                    grandFemaleMinority += femaleMinority;

                    worksheet.Cells[startRow + stt, 1].Value = stt;
                    worksheet.Cells[startRow + stt, 2].Value = $"Khối {grade}";
                    worksheet.Cells[startRow + stt, 3].Value = classCount;
                    worksheet.Cells[startRow + stt, 4].Value = total;
                    worksheet.Cells[startRow + stt, 5].Value = female;
                    worksheet.Cells[startRow + stt, 6].Value = minority;
                    worksheet.Cells[startRow + stt, 7].Value = femaleMinority;
                    worksheet.Cells[startRow + stt, 8].Value = femaleRatio;

                    stt++;
                }

                // Grand total
                double grandFemaleRatio = grandTotal > 0 ? Math.Round((double)grandFemale / grandTotal * 100, 1) : 0;
                int totalRow = startRow + stt;
                worksheet.Cells[totalRow, 1].Value = "";
                worksheet.Cells[totalRow, 2].Value = "TỔNG CỘNG";
                worksheet.Cells[totalRow, 3].Value = "";
                worksheet.Cells[totalRow, 4].Value = grandTotal;
                worksheet.Cells[totalRow, 5].Value = grandFemale;
                worksheet.Cells[totalRow, 6].Value = grandMinority;
                worksheet.Cells[totalRow, 7].Value = grandFemaleMinority;
                worksheet.Cells[totalRow, 8].Value = grandFemaleRatio;

                using (var r = worksheet.Cells[totalRow, 1, totalRow, 8])
                {
                    r.Style.Font.Bold = true;
                    r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    r.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                worksheet.Cells[startRow, 1, totalRow, 8].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[startRow, 1, totalRow, 8].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[startRow, 1, totalRow, 8].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[startRow, 1, totalRow, 8].Style.Border.Right.Style = ExcelBorderStyle.Thin;

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                package.SaveAs(new FileInfo(outputPath));

                Log.Information("[MOET] Đã xuất Mẫu 20-THCS Excel: {Path}", outputPath);
                return outputPath;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[MOET] Lỗi xuất Mẫu 20-THCS Excel");
                throw;
            }
        }

        public string GenerateMau22Excel(string schoolYear = "2025-2026", string semester = "HK2")
        {
            schoolYear = GetSchoolYearSetting(schoolYear);
            try
            {
                var activeRosterIds = _db.ClassRosters
                    .Where(r => r.SchoolYear == schoolYear && r.Semester == semester && r.IsActive)
                    .Select(r => r.Id)
                    .ToList();

                var studentIds = _db.ClassRosterStudents
                    .Where(crs => activeRosterIds.Contains(crs.RosterId))
                    .Select(crs => crs.StudentId)
                    .Distinct()
                    .ToList();

                var students = _db.Students
                    .Where(s => s.Status == "Active" && studentIds.Contains(s.Id))
                    .ToList();

                var gradeLevels = students.Select(s => ExtractGradeLevel(s.ClassName)).Distinct().OrderBy(g => g).ToList();
                var gradingService = new TT22GradingService(_db);

                string outputPath = Path.Combine(PdfTemplateHelper.GetOutputDirectory(), $"MOET_Mau22_THCS_{schoolYear}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

                ExcelPackage.License.SetNonCommercialOrganization("QA SmartClass");
                using var package = new ExcelPackage();

                // Sheet 1: Học Lực
                var ws1 = package.Workbook.Worksheets.Add("Học Lực");
                ws1.Cells[1, 1].Value = "BIỂU MẪU 22-THCS - KẾT QUẢ XẾP LOẠI HỌC LỰC";
                ws1.Cells[1, 1].Style.Font.Bold = true;
                ws1.Cells[2, 1].Value = $"Năm học: {schoolYear} | Học kỳ: {semester}";
                ws1.Cells[2, 1].Style.Font.Italic = true;

                int startRow = 4;
                ws1.Cells[startRow, 1].Value = "STT";
                ws1.Cells[startRow, 2].Value = "Khối";
                ws1.Cells[startRow, 3].Value = "Tổng HS";
                ws1.Cells[startRow, 4].Value = "Tốt";
                ws1.Cells[startRow, 5].Value = "Khá";
                ws1.Cells[startRow, 6].Value = "Đạt";
                ws1.Cells[startRow, 7].Value = "Chưa đạt";
                ws1.Cells[startRow, 8].Value = "% T+K";

                using (var r = ws1.Cells[startRow, 1, startRow, 8])
                {
                    r.Style.Font.Bold = true;
                    r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    r.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    r.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                int stt = 1;
                int totalStudents = 0, totalTot = 0, totalKha = 0, totalDat = 0, totalChuaDat = 0;
                foreach (var grade in gradeLevels)
                {
                    var gradeStudents = students.Where(s => ExtractGradeLevel(s.ClassName) == grade).ToList();
                    var stats = CalculateAcademicStats(gradeStudents, gradingService, schoolYear, semester);

                    ws1.Cells[startRow + stt, 1].Value = stt;
                    ws1.Cells[startRow + stt, 2].Value = $"Khối {grade}";
                    ws1.Cells[startRow + stt, 3].Value = stats.Total;
                    ws1.Cells[startRow + stt, 4].Value = stats.Tot;
                    ws1.Cells[startRow + stt, 5].Value = stats.Kha;
                    ws1.Cells[startRow + stt, 6].Value = stats.Dat;
                    ws1.Cells[startRow + stt, 7].Value = stats.ChuaDat;
                    ws1.Cells[startRow + stt, 8].Value = stats.TotKhaPercent;

                    totalStudents += stats.Total;
                    totalTot += stats.Tot;
                    totalKha += stats.Kha;
                    totalDat += stats.Dat;
                    totalChuaDat += stats.ChuaDat;

                    stt++;
                }

                // Add Grand Total Row
                int totalRow = startRow + stt;
                ws1.Cells[totalRow, 1].Value = "";
                ws1.Cells[totalRow, 2].Value = "TỔNG CỘNG";
                ws1.Cells[totalRow, 3].Value = totalStudents;
                ws1.Cells[totalRow, 4].Value = totalTot;
                ws1.Cells[totalRow, 5].Value = totalKha;
                ws1.Cells[totalRow, 6].Value = totalDat;
                ws1.Cells[totalRow, 7].Value = totalChuaDat;
                ws1.Cells[totalRow, 8].Value = totalStudents > 0 ? Math.Round((double)(totalTot + totalKha) / totalStudents * 100, 1) : 0;

                using (var r = ws1.Cells[totalRow, 1, totalRow, 8])
                {
                    r.Style.Font.Bold = true;
                    r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    r.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                ws1.Cells[startRow, 1, totalRow, 8].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                ws1.Cells[startRow, 1, totalRow, 8].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                ws1.Cells[startRow, 1, totalRow, 8].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                ws1.Cells[startRow, 1, totalRow, 8].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                ws1.Cells[ws1.Dimension.Address].AutoFitColumns();

                // Sheet 2: Hạnh Kiểm
                var ws2 = package.Workbook.Worksheets.Add("Hạnh Kiểm");
                ws2.Cells[1, 1].Value = "BIỂU MẪU 22-THCS - KẾT QUẢ XẾP LOẠI HẠNH KIỂM";
                ws2.Cells[1, 1].Style.Font.Bold = true;
                ws2.Cells[2, 1].Value = $"Năm học: {schoolYear} | Học kỳ: {semester}";
                ws2.Cells[2, 1].Style.Font.Italic = true;

                ws2.Cells[startRow, 1].Value = "STT";
                ws2.Cells[startRow, 2].Value = "Khối";
                ws2.Cells[startRow, 3].Value = "Tổng HS";
                ws2.Cells[startRow, 4].Value = "Tốt";
                ws2.Cells[startRow, 5].Value = "Khá";
                ws2.Cells[startRow, 6].Value = "Đạt";
                ws2.Cells[startRow, 7].Value = "Chưa đạt";
                ws2.Cells[startRow, 8].Value = "% Tốt+Khá";

                using (var r = ws2.Cells[startRow, 1, startRow, 8])
                {
                    r.Style.Font.Bold = true;
                    r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    r.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    r.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                stt = 1;
                int totalHS = 0, totalTotC = 0, totalKhaC = 0, totalDatC = 0, totalChuaDatC = 0;
                foreach (var grade in gradeLevels)
                {
                    var gradeStudents = students.Where(s => ExtractGradeLevel(s.ClassName) == grade).ToList();
                    int total = gradeStudents.Count;
                    int tot = gradeStudents.Count(s => s.ConductScore >= 80);
                    int kha = gradeStudents.Count(s => s.ConductScore >= 65 && s.ConductScore < 80);
                    int dat = gradeStudents.Count(s => s.ConductScore >= 50 && s.ConductScore < 65);
                    int chuadat = gradeStudents.Count(s => s.ConductScore < 50);
                    double tkPercent = total > 0 ? Math.Round((double)(tot + kha) / total * 100, 1) : 0;

                    ws2.Cells[startRow + stt, 1].Value = stt;
                    ws2.Cells[startRow + stt, 2].Value = $"Khối {grade}";
                    ws2.Cells[startRow + stt, 3].Value = total;
                    ws2.Cells[startRow + stt, 4].Value = tot;
                    ws2.Cells[startRow + stt, 5].Value = kha;
                    ws2.Cells[startRow + stt, 6].Value = dat;
                    ws2.Cells[startRow + stt, 7].Value = chuadat;
                    ws2.Cells[startRow + stt, 8].Value = tkPercent;

                    totalHS += total;
                    totalTotC += tot;
                    totalKhaC += kha;
                    totalDatC += dat;
                    totalChuaDatC += chuadat;

                    stt++;
                }

                // Add Grand Total Row
                totalRow = startRow + stt;
                ws2.Cells[totalRow, 1].Value = "";
                ws2.Cells[totalRow, 2].Value = "TỔNG CỘNG";
                ws2.Cells[totalRow, 3].Value = totalHS;
                ws2.Cells[totalRow, 4].Value = totalTotC;
                ws2.Cells[totalRow, 5].Value = totalKhaC;
                ws2.Cells[totalRow, 6].Value = totalDatC;
                ws2.Cells[totalRow, 7].Value = totalChuaDatC;
                ws2.Cells[totalRow, 8].Value = totalHS > 0 ? Math.Round((double)(totalTotC + totalKhaC) / totalHS * 100, 1) : 0;

                using (var r = ws2.Cells[totalRow, 1, totalRow, 8])
                {
                    r.Style.Font.Bold = true;
                    r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    r.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                ws2.Cells[startRow, 1, totalRow, 8].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                ws2.Cells[startRow, 1, totalRow, 8].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                ws2.Cells[startRow, 1, totalRow, 8].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                ws2.Cells[startRow, 1, totalRow, 8].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                ws2.Cells[ws2.Dimension.Address].AutoFitColumns();

                package.SaveAs(new FileInfo(outputPath));
                Log.Information("[MOET] Đã xuất Mẫu 22-THCS Excel: {Path}", outputPath);
                return outputPath;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[MOET] Lỗi xuất Mẫu 22-THCS Excel");
                throw;
            }
        }

        public string GenerateSummaryReportExcel(string schoolYear = "2025-2026")
        {
            schoolYear = GetSchoolYearSetting(schoolYear);
            try
            {
                var activeRosterIds = _db.ClassRosters
                    .Where(r => r.SchoolYear == schoolYear && r.IsActive)
                    .Select(r => r.Id)
                    .ToList();

                var studentIds = _db.ClassRosterStudents
                    .Where(crs => activeRosterIds.Contains(crs.RosterId))
                    .Select(crs => crs.StudentId)
                    .Distinct()
                    .ToList();

                var students = _db.Students
                    .Where(s => s.Status == "Active" && studentIds.Contains(s.Id))
                    .ToList();

                var teachers = _db.TeacherProfiles.Where(t => t.IsActive).ToList();

                string outputPath = Path.Combine(PdfTemplateHelper.GetOutputDirectory(), $"MOET_TongKet_{schoolYear}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

                ExcelPackage.License.SetNonCommercialOrganization("QA SmartClass");
                using var package = new ExcelPackage();

                // Sheet 1: Đội Ngũ Giáo Viên
                var ws1 = package.Workbook.Worksheets.Add("Đội Ngũ GV");
                ws1.Cells[1, 1].Value = "BÁO CÁO TỔNG KẾT - TÌNH HÌNH ĐỘI NGŨ GIÁO VIÊN";
                ws1.Cells[1, 1].Style.Font.Bold = true;
                ws1.Cells[2, 1].Value = $"Năm học: {schoolYear}";
                ws1.Cells[2, 1].Style.Font.Italic = true;

                int startRow = 4;
                ws1.Cells[startRow, 1].Value = "STT";
                ws1.Cells[startRow, 2].Value = "Trình độ";
                ws1.Cells[startRow, 3].Value = "Số lượng";
                ws1.Cells[startRow, 4].Value = "Tỷ lệ (%)";

                using (var r = ws1.Cells[startRow, 1, startRow, 4])
                {
                    r.Style.Font.Bold = true;
                    r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    r.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    r.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                var titleGroups = new[] { "GV", "ThS", "TS", "PGS", "GS" };
                var titleLabels = new Dictionary<string, string>
                {
                    { "GV", "Cử nhân / Đại học" },
                    { "ThS", "Thạc sĩ" },
                    { "TS", "Tiến sĩ" },
                    { "PGS", "Phó Giáo sư" },
                    { "GS", "Giáo sư" }
                };

                int stt = 1;
                foreach (var title in titleGroups)
                {
                    int count = teachers.Count(t => t.Title == title);
                    if (count == 0 && title != "GV" && title != "ThS") continue;
                    double pct = teachers.Count > 0 ? Math.Round((double)count / teachers.Count * 100, 1) : 0;

                    ws1.Cells[startRow + stt, 1].Value = stt;
                    ws1.Cells[startRow + stt, 2].Value = titleLabels.GetValueOrDefault(title, title);
                    ws1.Cells[startRow + stt, 3].Value = count;
                    ws1.Cells[startRow + stt, 4].Value = pct;

                    stt++;
                }

                ws1.Cells[startRow, 1, startRow + stt - 1, 4].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                ws1.Cells[startRow, 1, startRow + stt - 1, 4].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                ws1.Cells[startRow, 1, startRow + stt - 1, 4].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                ws1.Cells[startRow, 1, startRow + stt - 1, 4].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                ws1.Cells[ws1.Dimension.Address].AutoFitColumns();

                // Sheet 2: Học Sinh
                var ws2 = package.Workbook.Worksheets.Add("Học Sinh");
                ws2.Cells[1, 1].Value = "BÁO CÁO TỔNG KẾT - TÌNH HÌNH HỌC SINH";
                ws2.Cells[1, 1].Style.Font.Bold = true;
                ws2.Cells[2, 1].Value = $"Năm học: {schoolYear}";
                ws2.Cells[2, 1].Style.Font.Italic = true;

                var gradeLevels = students.Select(s => ExtractGradeLevel(s.ClassName)).Distinct().OrderBy(g => g).ToList();

                ws2.Cells[startRow, 1].Value = "STT";
                ws2.Cells[startRow, 2].Value = "Khối lớp";
                ws2.Cells[startRow, 3].Value = "Số lớp";
                ws2.Cells[startRow, 4].Value = "Sĩ số";
                ws2.Cells[startRow, 5].Value = "Nữ";
                ws2.Cells[startRow, 6].Value = "DTTS";

                using (var r = ws2.Cells[startRow, 1, startRow, 6])
                {
                    r.Style.Font.Bold = true;
                    r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    r.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    r.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                stt = 1;
                int totalStudents = 0, totalFemale = 0, totalMinority = 0;
                foreach (var grade in gradeLevels)
                {
                    var gs = students.Where(s => ExtractGradeLevel(s.ClassName) == grade).ToList();
                    int classCount = gs.Select(s => s.ClassName).Distinct().Count();
                    int female = gs.Count(s => s.Gender == "Nữ");
                    int minority = gs.Count(s => !string.IsNullOrWhiteSpace(s.Ethnicity) && !s.Ethnicity.Trim().Equals("Kinh", StringComparison.OrdinalIgnoreCase));

                    totalStudents += gs.Count;
                    totalFemale += female;
                    totalMinority += minority;

                    ws2.Cells[startRow + stt, 1].Value = stt;
                    ws2.Cells[startRow + stt, 2].Value = $"Khối {grade}";
                    ws2.Cells[startRow + stt, 3].Value = classCount;
                    ws2.Cells[startRow + stt, 4].Value = gs.Count;
                    ws2.Cells[startRow + stt, 5].Value = female;
                    ws2.Cells[startRow + stt, 6].Value = minority;
                    stt++;
                }

                // Total Row
                int totalRow = startRow + stt;
                ws2.Cells[totalRow, 1].Value = "";
                ws2.Cells[totalRow, 2].Value = "TỔNG CỘNG";
                ws2.Cells[totalRow, 3].Value = "";
                ws2.Cells[totalRow, 4].Value = totalStudents;
                ws2.Cells[totalRow, 5].Value = totalFemale;
                ws2.Cells[totalRow, 6].Value = totalMinority;

                using (var r = ws2.Cells[totalRow, 1, totalRow, 6])
                {
                    r.Style.Font.Bold = true;
                    r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    r.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                ws2.Cells[startRow, 1, totalRow, 6].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                ws2.Cells[startRow, 1, totalRow, 6].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                ws2.Cells[startRow, 1, totalRow, 6].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                ws2.Cells[startRow, 1, totalRow, 6].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                ws2.Cells[ws2.Dimension.Address].AutoFitColumns();

                package.SaveAs(new FileInfo(outputPath));
                Log.Information("[MOET] Đã xuất Báo cáo Tổng kết Excel: {Path}", outputPath);
                return outputPath;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[MOET] Lỗi xuất Báo cáo Tổng kết Excel");
                throw;
            }
        }
    }
}


