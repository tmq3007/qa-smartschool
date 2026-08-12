using System;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace QASmartClass.Services
{
    /// <summary>
    /// WI-01: Helper class tạo template QuestPDF chung cho tất cả báo cáo.
    /// Cung cấp: Header trường, footer số trang, style bảng, font Tiếng Việt.
    /// </summary>
    public static class PdfTemplateHelper
    {
        // --- CẤU HÌNH ---
        private const string DefaultSchoolName = "TRƯỜNG THCS/THPT ...";
        private const string DefaultSchoolAddress = "Địa chỉ: ...";
        private const string DefaultDepartment = "SỞ GIÁO DỤC VÀ ĐÀO TẠO ...";

        /// <summary>
        /// Thư mục output mặc định cho các báo cáo PDF.
        /// </summary>
        public static string GetOutputDirectory()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "QASmartClass", "Reports");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// Tạo đường dẫn file PDF với timestamp để tránh trùng.
        /// </summary>
        public static string GetOutputPath(string prefix, string suffix = "")
        {
            string safeSuffix = string.IsNullOrWhiteSpace(suffix) ? "" : $"_{SanitizeFileName(suffix)}";
            string fileName = $"{prefix}{safeSuffix}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return Path.Combine(GetOutputDirectory(), fileName);
        }

        /// <summary>
        /// Compose header chuẩn cho báo cáo trường học.
        /// </summary>
        public static void ComposeHeader(IContainer container, string reportTitle, string? subtitle = null)
        {
            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    // Cột trái: Sở GD&ĐT
                    row.RelativeItem(4).Column(left =>
                    {
                        left.Item().AlignCenter().Text(DefaultDepartment)
                            .FontSize(9).Italic();
                        left.Item().AlignCenter().Text(DefaultSchoolName)
                            .FontSize(11).Bold();
                    });

                    // Cột phải: CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM
                    row.RelativeItem(5).Column(right =>
                    {
                        right.Item().AlignCenter().Text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM")
                            .FontSize(10).Bold();
                        right.Item().AlignCenter().Text("Độc lập — Tự do — Hạnh phúc")
                            .FontSize(10).Italic().Underline();
                    });
                });

                col.Item().PaddingTop(15).AlignCenter().Text(reportTitle)
                    .FontSize(16).Bold();

                if (!string.IsNullOrEmpty(subtitle))
                {
                    col.Item().AlignCenter().Text(subtitle)
                        .FontSize(11).Italic();
                }

                col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);
            });
        }

        /// <summary>
        /// Compose footer chuẩn với số trang.
        /// </summary>
        public static void ComposeFooter(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().AlignLeft().Text(text =>
                {
                    text.Span("Ngày xuất: ").FontSize(8).Italic();
                    text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(8);
                });

                row.RelativeItem().AlignCenter().Text(text =>
                {
                    text.Span("QA SmartSchool").FontSize(8).Italic().FontColor(Colors.Grey.Medium);
                });

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Trang ").FontSize(8);
                    text.CurrentPageNumber().FontSize(8);
                    text.Span(" / ").FontSize(8);
                    text.TotalPages().FontSize(8);
                });
            });
        }

        /// <summary>
        /// Compose phần chữ ký cuối báo cáo.
        /// </summary>
        public static void ComposeSignatureBlock(IContainer container,
            string? leftTitle = "Người lập", string? rightTitle = "Hiệu trưởng")
        {
            container.PaddingTop(30).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().AlignCenter().Text(leftTitle ?? "Người lập").FontSize(10).Bold();
                    col.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").FontSize(8).Italic();
                    col.Item().Height(60); // Khoảng trống cho chữ ký
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().AlignCenter().Text($"..., ngày {DateTime.Now:dd} tháng {DateTime.Now:MM} năm {DateTime.Now:yyyy}")
                        .FontSize(9).Italic();
                    col.Item().AlignCenter().Text(rightTitle ?? "Hiệu trưởng").FontSize(10).Bold();
                    col.Item().AlignCenter().Text("(Ký, đóng dấu)").FontSize(8).Italic();
                    col.Item().Height(60);
                });
            });
        }

        /// <summary>
        /// Style cho header bảng.
        /// </summary>
        public static IContainer TableHeaderStyle(IContainer container)
        {
            return container
                .Background(Colors.Blue.Darken3)
                .Padding(5)
                .DefaultTextStyle(t => t.FontSize(9).FontColor(Colors.White).Bold());
        }

        /// <summary>
        /// Style cho cell bảng (xen kẽ màu).
        /// </summary>
        public static IContainer TableCellStyle(IContainer container, bool isAlternate)
        {
            var bg = isAlternate ? Colors.Grey.Lighten4 : Colors.White;
            return container
                .Background(bg)
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(4)
                .DefaultTextStyle(t => t.FontSize(9));
        }

        /// <summary>
        /// Sanitize tên file, loại bỏ ký tự không hợp lệ.
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Replace(' ', '_');
        }
    }
}
