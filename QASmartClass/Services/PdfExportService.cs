using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QASmartClass.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Serilog;

namespace QASmartClass.Services
{
    public class PdfExportService
    {
        private readonly AppDbContext _db;

        public PdfExportService(AppDbContext db)
        {
            _db = db;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public string ExportAttendanceReport(string className, DateTime date, List<Student> allStudents, List<int> presentStudentIds)
        {
            var fileName = $"Attendance_{className}_{date:yyyyMMdd_HHmmss}.pdf";
            var filePath = Path.Combine(AppPaths.ExportsDir, fileName);

            try
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                        page.Header().Element(c => ComposeHeader(c, "BÁO CÁO ĐIỂM DANH", className, date));
                        page.Content().Element(c => ComposeAttendanceContent(c, allStudents, presentStudentIds));
                        page.Footer().Element(ComposeFooter);
                    });
                })
                .GeneratePdf(filePath);

                Log.Information("[Export] Generated Attendance PDF: {Path}", filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                Log.Error("[Export] Error generating attendance PDF: {Err}", ex.Message);
                return string.Empty;
            }
        }

        public string ExportGradeReport(string className, string subject, List<StudentGradeDto> grades)
        {
            var fileName = $"Grades_{className}_{subject}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var filePath = Path.Combine(AppPaths.ExportsDir, fileName);

            try
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                        page.Header().Element(c => ComposeHeader(c, $"BẢNG ĐIỂM - MÔN {subject.ToUpper()}", className, DateTime.Now));
                        page.Content().Element(c => ComposeGradeContent(c, grades));
                        page.Footer().Element(ComposeFooter);
                    });
                })
                .GeneratePdf(filePath);

                Log.Information("[Export] Generated Grade PDF: {Path}", filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                Log.Error("[Export] Error generating grade PDF: {Err}", ex.Message);
                return string.Empty;
            }
        }

        public string GenerateHomeroomReport(string className, string semester, List<StudentGradeDto> grades, int totalAbsences, int totalDiscipline, string emulationRank)
        {
            var fileName = $"Homeroom_{className}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var filePath = Path.Combine(AppPaths.ExportsDir, fileName);

            try
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                        page.Header().Element(c => ComposeHeader(c, $"BÁO CÁO TỔNG KẾT CHỦ NHIỆM", className, DateTime.Now));
                        page.Content().Element(c => ComposeHomeroomContent(c, semester, grades, totalAbsences, totalDiscipline, emulationRank));
                        page.Footer().Element(ComposeFooter);
                    });
                })
                .GeneratePdf(filePath);

                Log.Information("[Export] Generated Homeroom PDF: {Path}", filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                Log.Error("[Export] Error generating homeroom PDF: {Err}", ex.Message);
                return string.Empty;
            }
        }

        private void ComposeHomeroomContent(IContainer container, string semester, List<StudentGradeDto> grades, int totalAbsences, int totalDiscipline, string emulationRank)
        {
            container.PaddingVertical(0.5f, Unit.Centimetre).Column(col =>
            {
                col.Item().Text($"Học kỳ: {semester}").SemiBold();
                col.Item().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text("Tổng số ngày vắng").FontColor(Colors.Grey.Medium);
                        c.Item().Text(totalAbsences.ToString()).FontSize(20).Bold().FontColor(Colors.Red.Medium);
                    });
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text("Tổng số kỷ luật/nhắc nhở").FontColor(Colors.Grey.Medium);
                        c.Item().Text(totalDiscipline.ToString()).FontSize(20).Bold().FontColor(Colors.Orange.Darken2);
                    });
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text("Thi đua").FontColor(Colors.Grey.Medium);
                        c.Item().Text(emulationRank).FontSize(16).Bold().FontColor(Colors.Green.Darken2);
                    });
                });

                col.Item().PaddingTop(16).Text("Bảng điểm học tập của lớp").FontSize(13).SemiBold();
                col.Item().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);
                        columns.ConstantColumn(100);
                        columns.RelativeColumn();
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(80);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("STT");
                        header.Cell().Element(HeaderStyle).Text("Mã HS");
                        header.Cell().Element(HeaderStyle).Text("Họ và Tên");
                        header.Cell().Element(HeaderStyle).Text("Điểm số");
                        header.Cell().Element(HeaderStyle).Text("Xếp loại");
                    });

                    int i = 1;
                    foreach (var g in grades.OrderByDescending(x => x.Score))
                    {
                        string rank = g.Score >= 8 ? "Giỏi" : (g.Score >= 6.5 ? "Khá" : (g.Score >= 5 ? "TB" : "Yếu"));

                        table.Cell().Element(CellStyle).Text(i.ToString());
                        table.Cell().Element(CellStyle).Text(g.StudentCode);
                        table.Cell().Element(CellStyle).Text(g.FullName);
                        table.Cell().Element(CellStyle).Text(g.Score.ToString("F1")).Bold();
                        table.Cell().Element(CellStyle).Text(rank);
                        i++;
                    }
                });
            });
        }

        // --- Layout Composers ---

        private void ComposeHeader(IContainer container, string title, string className, DateTime date)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text(title).FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text($"Lớp: {className}").FontSize(14).Medium();
                    column.Item().Text($"Ngày xuất: {date:dd/MM/yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Medium);
                });

                row.ConstantItem(80).AlignRight().Text("QA SMART").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Text(x =>
            {
                x.Span("Trang ");
                x.CurrentPageNumber();
                x.Span(" / ");
                x.TotalPages();
                x.Span(" - Xuất từ QA SmartClass");
            });
        }

        private void ComposeAttendanceContent(IContainer container, List<Student> students, List<int> presentIds)
        {
            container.PaddingVertical(1, Unit.Centimetre).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(40);
                    columns.ConstantColumn(100);
                    columns.RelativeColumn();
                    columns.ConstantColumn(100);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderStyle).Text("STT");
                    header.Cell().Element(HeaderStyle).Text("Mã HS");
                    header.Cell().Element(HeaderStyle).Text("Họ và Tên");
                    header.Cell().Element(HeaderStyle).Text("Trạng thái");
                });

                int i = 1;
                foreach (var student in students.OrderBy(s => s.FullName))
                {
                    bool isPresent = presentIds.Contains(student.Id);
                    
                    table.Cell().Element(CellStyle).Text(i.ToString());
                    table.Cell().Element(CellStyle).Text(student.StudentCode);
                    table.Cell().Element(CellStyle).Text(student.FullName);
                    table.Cell().Element(CellStyle).Text(isPresent ? "Có mặt" : "Vắng").FontColor(isPresent ? Colors.Green.Medium : Colors.Red.Medium);
                    i++;
                }
            });
        }

        private void ComposeGradeContent(IContainer container, List<StudentGradeDto> grades)
        {
            container.PaddingVertical(1, Unit.Centimetre).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(40);
                    columns.ConstantColumn(100);
                    columns.RelativeColumn();
                    columns.ConstantColumn(80);
                    columns.ConstantColumn(80);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderStyle).Text("STT");
                    header.Cell().Element(HeaderStyle).Text("Mã HS");
                    header.Cell().Element(HeaderStyle).Text("Họ và Tên");
                    header.Cell().Element(HeaderStyle).Text("Điểm số");
                    header.Cell().Element(HeaderStyle).Text("Xếp loại");
                });

                int i = 1;
                foreach (var g in grades.OrderByDescending(x => x.Score))
                {
                    string rank = g.Score >= 8 ? "Giỏi" : (g.Score >= 6.5 ? "Khá" : (g.Score >= 5 ? "TB" : "Yếu"));
                    
                    table.Cell().Element(CellStyle).Text(i.ToString());
                    table.Cell().Element(CellStyle).Text(g.StudentCode);
                    table.Cell().Element(CellStyle).Text(g.FullName);
                    table.Cell().Element(CellStyle).Text(g.Score.ToString("F1")).Bold();
                    table.Cell().Element(CellStyle).Text(rank);
                    i++;
                }
            });
        }

        private static IContainer HeaderStyle(IContainer container)
        {
            return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
        }

        // ─── TEMPLATE 3: QUIZ SUMMARY ────────────────────────────────────────────

        /// <summary>
        /// Xuất báo cáo thống kê kết quả một bài Quiz cụ thể.
        /// </summary>
        public string ExportQuizSummaryReport(QuizSummaryDto quiz)
        {
            var fileName = $"QuizSummary_{quiz.QuizTitle.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var filePath = Path.Combine(AppPaths.ExportsDir, fileName);

            try
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                        page.Header().Element(c => ComposeHeader(c, "THỐNG KÊ KẾT QUẢ KIỂM TRA", quiz.ClassName, DateTime.Now));
                        page.Content().Element(c => ComposeQuizSummaryContent(c, quiz));
                        page.Footer().Element(ComposeFooter);
                    });
                })
                .GeneratePdf(filePath);

                Log.Information("[Export] Generated Quiz Summary PDF: {Path}", filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                Log.Error("[Export] Error generating quiz summary PDF: {Err}", ex.Message);
                return string.Empty;
            }
        }

        private void ComposeQuizSummaryContent(IContainer container, QuizSummaryDto quiz)
        {
            container.PaddingVertical(0.5f, Unit.Centimetre).Column(col =>
            {
                // ── Thông tin bài quiz ──────────────────────────────
                col.Item().Border(1).BorderColor(Colors.Blue.Lighten3)
                    .Background(Colors.Blue.Lighten5).Padding(10).Column(info =>
                {
                    info.Item().Text($"Tên bài kiểm tra: {quiz.QuizTitle}").SemiBold();
                    info.Item().Text($"Loại: {quiz.QuizType}   |   Thời gian: {quiz.TimeLimitSeconds / 60} phút   |   Tổng câu: {quiz.TotalQuestions}");
                    info.Item().Text($"Ngày thi: {quiz.HeldAt:dd/MM/yyyy HH:mm}   |   Số lượt nộp: {quiz.Results.Count}");
                });

                col.Item().PaddingTop(12).Row(row =>
                {
                    // Điểm TB
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text("Điểm trung bình").FontColor(Colors.Grey.Medium);
                        double avg = quiz.Results.Count > 0 ? quiz.Results.Average(r => r.ScorePercent) : 0;
                        c.Item().Text($"{avg:F1}%").FontSize(24).Bold().FontColor(Colors.Blue.Darken2);
                    });
                    // Tỉ lệ đạt
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text("Tỉ lệ đạt (≥50%)").FontColor(Colors.Grey.Medium);
                        int passed = quiz.Results.Count(r => r.ScorePercent >= 50);
                        double passRate = quiz.Results.Count > 0 ? (double)passed / quiz.Results.Count * 100 : 0;
                        c.Item().Text($"{passRate:F0}%").FontSize(24).Bold().FontColor(Colors.Green.Darken2);
                    });
                    // Điểm cao nhất
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text("Điểm cao nhất").FontColor(Colors.Grey.Medium);
                        double max = quiz.Results.Count > 0 ? quiz.Results.Max(r => r.ScorePercent) : 0;
                        c.Item().Text($"{max:F1}%").FontSize(24).Bold().FontColor(Colors.Orange.Darken2);
                    });
                    // Điểm thấp nhất
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text("Điểm thấp nhất").FontColor(Colors.Grey.Medium);
                        double min = quiz.Results.Count > 0 ? quiz.Results.Min(r => r.ScorePercent) : 0;
                        c.Item().Text($"{min:F1}%").FontSize(24).Bold().FontColor(Colors.Red.Medium);
                    });
                });

                // ── Phân loại kết quả ──────────────────────────────
                col.Item().PaddingTop(12).Text("Phân loại kết quả").FontSize(13).SemiBold();
                col.Item().PaddingTop(4).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);
                        columns.RelativeColumn();
                        columns.ConstantColumn(100);
                        columns.ConstantColumn(100);
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(80);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("STT");
                        header.Cell().Element(HeaderStyle).Text("Họ và Tên");
                        header.Cell().Element(HeaderStyle).Text("Đúng / Tổng");
                        header.Cell().Element(HeaderStyle).Text("Thời gian");
                        header.Cell().Element(HeaderStyle).Text("Điểm %");
                        header.Cell().Element(HeaderStyle).Text("Xếp loại");
                    });

                    int i = 1;
                    foreach (var r in quiz.Results.OrderByDescending(x => x.ScorePercent))
                    {
                        string rank = r.ScorePercent >= 80 ? "Giỏi"
                            : r.ScorePercent >= 65 ? "Khá"
                            : r.ScorePercent >= 50 ? "TB"
                            : "Yếu";
                        var rankColor = r.ScorePercent >= 80 ? Colors.Green.Darken2
                            : r.ScorePercent >= 50 ? Colors.Blue.Medium
                            : Colors.Red.Medium;

                        table.Cell().Element(CellStyle).Text(i.ToString());
                        table.Cell().Element(CellStyle).Text(r.FullName);
                        table.Cell().Element(CellStyle).Text($"{r.CorrectCount} / {r.TotalQuestions}");
                        table.Cell().Element(CellStyle).Text($"{r.TimeSpentSeconds / 60:F0}p{r.TimeSpentSeconds % 60:F0}s");
                        table.Cell().Element(CellStyle).Text($"{r.ScorePercent:F1}%").Bold();
                        table.Cell().Element(CellStyle).Text(rank).FontColor(rankColor);
                        i++;
                    }
                });
            });
        }

        // ─── TEMPLATE 4: USAGE REPORT ────────────────────────────────────────────

        /// <summary>
        /// Xuất báo cáo thống kê hoạt động sử dụng hệ thống theo khoảng thời gian.
        /// </summary>
        public string ExportUsageReport(UsageReportDto report)
        {
            var fileName = $"UsageReport_{report.FromDate:yyyyMMdd}_{report.ToDate:yyyyMMdd}_{DateTime.Now:HHmmss}.pdf";
            var filePath = Path.Combine(AppPaths.ExportsDir, fileName);

            try
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                        page.Header().Element(c => ComposeHeader(c, "BÁO CÁO SỬ DỤNG HỆ THỐNG", "Toàn trường", DateTime.Now));
                        page.Content().Element(c => ComposeUsageContent(c, report));
                        page.Footer().Element(ComposeFooter);
                    });
                })
                .GeneratePdf(filePath);

                Log.Information("[Export] Generated Usage Report PDF: {Path}", filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                Log.Error("[Export] Error generating usage report PDF: {Err}", ex.Message);
                return string.Empty;
            }
        }

        private void ComposeUsageContent(IContainer container, UsageReportDto report)
        {
            container.PaddingVertical(0.5f, Unit.Centimetre).Column(col =>
            {
                // ── Khoảng thời gian ───────────────────────────────
                col.Item().Background(Colors.Grey.Lighten4).Padding(10).Text(
                    $"Khoảng thời gian: {report.FromDate:dd/MM/yyyy} — {report.ToDate:dd/MM/yyyy}"
                ).SemiBold();

                // ── KPI tổng quát ──────────────────────────────────
                col.Item().PaddingTop(12).Row(row =>
                {
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text("Tổng phiên làm việc").FontColor(Colors.Grey.Medium);
                        c.Item().Text(report.TotalSessions.ToString()).FontSize(24).Bold().FontColor(Colors.Blue.Darken2);
                    });
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text("Tổng giờ sử dụng").FontColor(Colors.Grey.Medium);
                        c.Item().Text($"{report.TotalHours:F1}h").FontSize(24).Bold().FontColor(Colors.Purple.Medium);
                    });
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text("Quiz đã thực hiện").FontColor(Colors.Grey.Medium);
                        c.Item().Text(report.TotalQuizzes.ToString()).FontSize(24).Bold().FontColor(Colors.Green.Darken2);
                    });
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text("File đã chuyển").FontColor(Colors.Grey.Medium);
                        c.Item().Text(report.TotalFileTransfers.ToString()).FontSize(24).Bold().FontColor(Colors.Orange.Darken2);
                    });
                });

                // ── Top sự kiện phổ biến ───────────────────────────
                if (report.TopEvents.Count > 0)
                {
                    col.Item().PaddingTop(16).Text("Các hoạt động phổ biến nhất").FontSize(13).SemiBold();
                    col.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(40);
                            columns.RelativeColumn();
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(120);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("STT");
                            header.Cell().Element(HeaderStyle).Text("Loại sự kiện");
                            header.Cell().Element(HeaderStyle).Text("Số lần");
                            header.Cell().Element(HeaderStyle).Text("Thời gian TB (ms)");
                        });

                        int i = 1;
                        foreach (var ev in report.TopEvents.Take(15))
                        {
                            table.Cell().Element(CellStyle).Text(i.ToString());
                            table.Cell().Element(CellStyle).Text(ev.EventType);
                            table.Cell().Element(CellStyle).Text(ev.Count.ToString()).Bold();
                            table.Cell().Element(CellStyle).Text(ev.AvgDurationMs > 0 ? $"{ev.AvgDurationMs:F0} ms" : "—");
                            i++;
                        }
                    });
                }

                // ── Hoạt động theo ngày ────────────────────────────
                if (report.DailyActivity.Count > 0)
                {
                    col.Item().PaddingTop(16).Text("Hoạt động theo ngày").FontSize(13).SemiBold();
                    col.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(40);
                            columns.ConstantColumn(120);
                            columns.RelativeColumn();
                            columns.ConstantColumn(100);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("STT");
                            header.Cell().Element(HeaderStyle).Text("Ngày");
                            header.Cell().Element(HeaderStyle).Text("Tổng sự kiện");
                            header.Cell().Element(HeaderStyle).Text("Thời gian (h)");
                        });

                        int i = 1;
                        foreach (var day in report.DailyActivity.OrderByDescending(d => d.Date))
                        {
                            table.Cell().Element(CellStyle).Text(i.ToString());
                            table.Cell().Element(CellStyle).Text(day.Date.ToString("dd/MM/yyyy"));
                            table.Cell().Element(CellStyle).Text(day.EventCount.ToString());
                            table.Cell().Element(CellStyle).Text($"{day.TotalHours:F1}h");
                            i++;
                        }
                    });
                }
            });
        }

        public string ExportLessonPlan(string subject, string grade, string title, string content, string? outputPath = null)
        {
            var fileName = $"GiaoAn_{subject.Replace(" ", "_")}_{grade.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var filePath = outputPath ?? Path.Combine(AppPaths.ExportsDir, fileName);

            try
            {
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                        page.Header().Element(c => ComposeHeader(c, "GIÁO ÁN BÀI GIẢNG", $"Môn: {subject} | Lớp: {grade}", DateTime.Now));
                        page.Content().Element(c => ComposeLessonPlanContent(c, title, content));
                        page.Footer().Element(ComposeFooter);
                    });
                })
                .GeneratePdf(filePath);

                Log.Information("[Export] Generated Lesson Plan PDF: {Path}", filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                Log.Error("[Export] Error generating lesson plan PDF: {Err}", ex.Message);
                return string.Empty;
            }
        }

        private void ComposeLessonPlanContent(IContainer container, string title, string content)
        {
            container.PaddingVertical(0.5f, Unit.Centimetre).Column(col =>
            {
                col.Item().Text($"Bài dạy: {title}").FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                col.Item().PaddingTop(10);

                var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        col.Item().PaddingTop(5);
                    }
                    else
                    {
                        col.Item().Text(line).LineHeight(1.3f);
                    }
                }
            });
        }

        public string ExportHealthRecordReport(HealthRecord r, Student student, List<HealthRecord> history)
        {
            var fileName = $"HealthRecord_{r.StudentName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var filePath = Path.Combine(AppPaths.ExportsDir, fileName);

            try
            {
                QuestPDF.Settings.License = LicenseType.Community;
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                        page.Header().Element(c => ComposeHeader(c, "PHIẾU SỨC KHỎE HỌC SINH", r.ClassName, r.ExamDate));
                        page.Content().Element(c => ComposeHealthRecordContent(c, r, student, history));
                        page.Footer().Element(ComposeFooter);
                    });
                })
                .GeneratePdf(filePath);

                Log.Information("[Export] Generated Health Record PDF: {Path}", filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                Log.Error("[Export] Error generating health record PDF: {Err}", ex.Message);
                return string.Empty;
            }
        }

        private void ComposeHealthRecordContent(QuestPDF.Infrastructure.IContainer container, HealthRecord r, Student student, List<HealthRecord> history)
        {
            double bmi = QASmartClass.Helpers.BmiCalculator.CalculateBmi(r.Height, r.Weight);

            int grade = 10;
            if (student != null)
            {
                var match = System.Text.RegularExpressions.Regex.Match(student.ClassName ?? "", @"\d+");
                if (match.Success)
                {
                    int.TryParse(match.Value, out grade);
                }
            }
            int age = r.ExactAge ?? Math.Max(6, Math.Min(18, grade + 5));

            var bmiStandardSetting = _db.SystemSettings.FirstOrDefault(s => s.Id == "Medical_BmiCalculationStandard");
            bool useChildStandard = (bmiStandardSetting?.Value ?? "1") == "1";

            string bmiCategory = QASmartClass.Helpers.BmiCalculator.GetBmiCategory(bmi, age, student?.Gender ?? "Nam", useChildStandard && student != null);

            container.PaddingVertical(0.5f, Unit.Centimetre).Column(col =>
            {
                // Thông tin chung học sinh
                col.Item().Text("THÔNG TIN HỌC SINH").FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                
                col.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text($"Họ và Tên: {r.StudentName}").Bold();
                        c.Item().Text($"Mã Học sinh: {student?.StudentCode ?? "N/A"}");
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text($"Lớp: {r.ClassName}");
                        c.Item().Text($"Giới tính: {student?.Gender ?? "N/A"}");
                    });
                });

                // Chỉ số đo khám y tế
                col.Item().PaddingTop(20).Text("CHỈ SỐ ĐO KHÁM Y TẾ").FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                col.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text("Chiều cao").FontColor(Colors.Grey.Medium);
                        c.Item().Text($"{r.Height} cm").FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                    });
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text("Cân nặng").FontColor(Colors.Grey.Medium);
                        c.Item().Text($"{r.Weight} kg").FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                    });
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text("Chỉ số BMI").FontColor(Colors.Grey.Medium);
                        c.Item().Text($"{bmi:F1} ({bmiCategory})").FontSize(16).Bold().FontColor(Colors.Green.Darken2);
                    });
                });

                col.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text("Thị lực Mắt Trái").FontColor(Colors.Grey.Medium);
                        double vl = r.VisionLeft > 2.0 ? r.VisionLeft / 10.0 : r.VisionLeft;
                        c.Item().Text($"{vl:F1}").FontSize(18).Bold().FontColor(Colors.Purple.Medium);
                    });
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text("Thị lực Mắt Phải").FontColor(Colors.Grey.Medium);
                        double vr = r.VisionRight > 2.0 ? r.VisionRight / 10.0 : r.VisionRight;
                        c.Item().Text($"{vr:F1}").FontSize(18).Bold().FontColor(Colors.Purple.Medium);
                    });
                });

                // Tiền sử & Ghi chú
                col.Item().PaddingTop(20).Text("TIỀN SỬ BỆNH & GHI CHÚ").FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                col.Item().PaddingTop(10).Text(string.IsNullOrWhiteSpace(r.ChronicConditions) ? "Không ghi nhận tiền sử bệnh mãn tính hay dị ứng." : r.ChronicConditions).Italic();

                // Lịch sử khám sức khỏe
                if (history != null && history.Count > 0)
                {
                    col.Item().PaddingTop(25).Text("LỊCH SỬ KHÁM SỨC KHỎE").FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(40);
                            columns.ConstantColumn(120);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("STT");
                            header.Cell().Element(HeaderStyle).Text("Ngày khám");
                            header.Cell().Element(HeaderStyle).Text("Chiều cao");
                            header.Cell().Element(HeaderStyle).Text("Cân nặng");
                            header.Cell().Element(HeaderStyle).Text("Thị lực (T/P)");
                        });

                        int idx = 1;
                        foreach (var hist in history.OrderByDescending(h => h.ExamDate))
                        {
                            table.Cell().Element(CellStyle).Text(idx.ToString());
                            table.Cell().Element(CellStyle).Text(hist.ExamDate.ToString("dd/MM/yyyy"));
                            table.Cell().Element(CellStyle).Text($"{hist.Height} cm");
                            table.Cell().Element(CellStyle).Text($"{hist.Weight} kg");
                            double hvl = hist.VisionLeft > 2.0 ? hist.VisionLeft / 10.0 : hist.VisionLeft;
                            double hvr = hist.VisionRight > 2.0 ? hist.VisionRight / 10.0 : hist.VisionRight;
                            table.Cell().Element(CellStyle).Text($"{hvl:F1} / {hvr:F1}");
                            idx++;
                        }
                    });
                }
            });
        }
    }

    // ──────────────────────────────────────────────────────────────
    // DTOs
    // ──────────────────────────────────────────────────────────────

    public class StudentGradeDto
    {
        public string StudentCode { get; set; } = "";
        public string FullName { get; set; } = "";
        public double Score { get; set; }
    }

    /// <summary>DTO cho Quiz Summary Report</summary>
    public class QuizSummaryDto
    {
        public string QuizTitle { get; set; } = "";
        public string QuizType { get; set; } = "";
        public string ClassName { get; set; } = "";
        public int TotalQuestions { get; set; }
        public int TimeLimitSeconds { get; set; }
        public DateTime HeldAt { get; set; } = DateTime.Now;
        public List<QuizResultRowDto> Results { get; set; } = new();
    }

    public class QuizResultRowDto
    {
        public string FullName { get; set; } = "";
        public int CorrectCount { get; set; }
        public int TotalQuestions { get; set; }
        public double ScorePercent { get; set; }
        public double TimeSpentSeconds { get; set; }
    }

    /// <summary>DTO cho Usage Report</summary>
    public class UsageReportDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalSessions { get; set; }
        public double TotalHours { get; set; }
        public int TotalQuizzes { get; set; }
        public int TotalFileTransfers { get; set; }
        public List<EventSummaryDto> TopEvents { get; set; } = new();
        public List<DailyActivityDto> DailyActivity { get; set; } = new();
    }

    public class EventSummaryDto
    {
        public string EventType { get; set; } = "";
        public int Count { get; set; }
        public double AvgDurationMs { get; set; }
    }

    public class DailyActivityDto
    {
        public DateTime Date { get; set; }
        public int EventCount { get; set; }
        public double TotalHours { get; set; }
    }
}

