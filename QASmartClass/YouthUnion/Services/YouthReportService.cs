using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.YouthUnion.Services
{
    public class YouthReportService
    {
        public async Task<string> GenerateMemberListPdfAsync(List<YouthMember> members, string outputPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    QuestPDF.Settings.License = LicenseType.Community;
                    var document = Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(2, Unit.Centimetre);
                            page.PageColor(Colors.White);
                            page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                            page.Header().Element(ComposeHeader);
                            page.Content().Element(x => ComposeContent(x, members));
                            page.Footer().AlignCenter().Text(x =>
                            {
                                x.Span("Trang ");
                                x.CurrentPageNumber();
                                x.Span(" / ");
                                x.TotalPages();
                            });
                        });
                    });

                    document.GeneratePdf(outputPath);
                    return outputPath;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Lỗi khi xuất PDF danh sách Đoàn viên");
                    throw;
                }
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("ĐOÀN TNCS HỒ CHÍ MINH").FontSize(14).Bold().AlignCenter();
                    column.Item().Text("BCH CHI ĐOÀN TRƯỜNG").FontSize(12).Bold().AlignCenter();
                    column.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Black);
                });

                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM").FontSize(12).Bold().AlignCenter();
                    column.Item().Text("Độc lập - Tự do - Hạnh phúc").FontSize(12).Bold().AlignCenter();
                    column.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Black);
                });
            });
        }

        private void ComposeContent(IContainer container, List<YouthMember> members)
        {
            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                column.Spacing(20);
                column.Item().Text("DANH SÁCH ĐOÀN VIÊN CHI ĐOÀN").FontSize(16).Bold().AlignCenter();

                column.Item().Table(table =>
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
                        header.Cell().Element(CellStyle).Text("STT");
                        header.Cell().Element(CellStyle).Text("Họ và Tên");
                        header.Cell().Element(CellStyle).Text("Lớp");
                        header.Cell().Element(CellStyle).Text("Chức vụ");
                        header.Cell().Element(CellStyle).Text("Trạng thái");

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.DefaultTextStyle(x => x.Bold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                        }
                    });

                    int i = 1;
                    foreach (var member in members)
                    {
                        table.Cell().Element(CellStyle).Text(i.ToString());
                        table.Cell().Element(CellStyle).Text(member.StudentName);
                        table.Cell().Element(CellStyle).Text(member.ClassName);
                        table.Cell().Element(CellStyle).Text(QASmartClass.YouthUnion.YouthMapper.MapPositionToUI(member.Position));
                        table.Cell().Element(CellStyle).Text(QASmartClass.YouthUnion.YouthMapper.MapStatusToUI(member.Status));

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                        }
                        i++;
                    }
                });
            });
        }
    }
}

