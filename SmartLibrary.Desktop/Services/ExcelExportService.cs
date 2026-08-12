using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace SmartLibrary.Desktop.Services
{
    /// <summary>
    /// Service xuất Excel .xlsx thực tế sử dụng ClosedXML (MIT license)
    /// </summary>
    public static class ExcelExportService
    {
        /// <summary>
        /// Xuất danh sách dữ liệu ra file .xlsx chuẩn.
        /// </summary>
        public static async Task ExportToXlsxAsync<T>(
            IEnumerable<T> data,
            string[] headers,
            Func<T, object[]> rowMapper,
            string filePath,
            string sheetTitle = "Data",
            string? customHeaderTitle = null)
        {
            await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add(sheetTitle);
                worksheet.Style.Font.SetFontName("Arial");

                // Ghi chú dòng tiêu đề / Metadata
                var metadataCell = worksheet.Cell(1, 1);
                metadataCell.Value = $"{customHeaderTitle ?? sheetTitle} - Exported {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                metadataCell.Style.Font.SetItalic(true);
                metadataCell.Style.Font.SetFontSize(10);
                metadataCell.Style.Font.SetFontColor(XLColor.Gray);

                // Column headers
                for (int col = 0; col < headers.Length; col++)
                {
                    var cell = worksheet.Cell(3, col + 1);
                    cell.Value = headers[col];
                    cell.Style.Font.SetBold(true);
                    cell.Style.Fill.SetBackgroundColor(XLColor.LightGray);
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                // Data rows
                int rowIdx = 4;
                foreach (var item in data)
                {
                    var values = rowMapper(item);
                    for (int col = 0; col < values.Length; col++)
                    {
                        var cell = worksheet.Cell(rowIdx, col + 1);
                        var val = values[col];
                        
                        SetCellValue(cell, val);
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    }
                    rowIdx++;
                }

                // Chèn chữ ký Kế toán, Người lập biểu, Hiệu trưởng ở Footer
                rowIdx += 2;
                var cell1 = worksheet.Cell(rowIdx, 2);
                cell1.Value = "Người lập biểu\n(Ký, ghi rõ họ tên)";
                cell1.Style.Font.SetItalic(true);
                cell1.Style.Font.SetBold(true);
                cell1.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                cell1.Style.Alignment.SetWrapText(true);

                var cell2 = worksheet.Cell(rowIdx, 5);
                cell2.Value = "Kế toán trưởng\n(Ký, ghi rõ họ tên)";
                cell2.Style.Font.SetItalic(true);
                cell2.Style.Font.SetBold(true);
                cell2.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                cell2.Style.Alignment.SetWrapText(true);

                var cell3 = worksheet.Cell(rowIdx, 8);
                cell3.Value = "Hiệu trưởng duyệt\n(Ký tên, đóng dấu)";
                cell3.Style.Font.SetItalic(true);
                cell3.Style.Font.SetBold(true);
                cell3.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                cell3.Style.Alignment.SetWrapText(true);

                worksheet.Row(rowIdx).Height = 45;

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(filePath);
            });
        }

        /// <summary>
        /// Xuất với progress callback cho UI.
        /// </summary>
        public static async Task ExportToXlsxWithProgressAsync<T>(
            IList<T> data,
            string[] headers,
            Func<T, object[]> rowMapper,
            string filePath,
            string sheetTitle,
            Action<int> progressCallback,
            string? customHeaderTitle = null)
        {
            await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add(sheetTitle);
                worksheet.Style.Font.SetFontName("Arial");

                // Ghi chú dòng tiêu đề / Metadata
                var metadataCell = worksheet.Cell(1, 1);
                metadataCell.Value = $"{customHeaderTitle ?? sheetTitle} - Exported {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                metadataCell.Style.Font.SetItalic(true);
                metadataCell.Style.Font.SetFontSize(10);
                metadataCell.Style.Font.SetFontColor(XLColor.Gray);

                // Column headers
                for (int col = 0; col < headers.Length; col++)
                {
                    var cell = worksheet.Cell(3, col + 1);
                    cell.Value = headers[col];
                    cell.Style.Font.SetBold(true);
                    cell.Style.Fill.SetBackgroundColor(XLColor.LightGray);
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                int total = data.Count;
                for (int i = 0; i < total; i++)
                {
                    var values = rowMapper(data[i]);
                    int rowIdx = i + 4;
                    for (int col = 0; col < values.Length; col++)
                    {
                        var cell = worksheet.Cell(rowIdx, col + 1);
                        var val = values[col];
                        
                        SetCellValue(cell, val);
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    }

                    // Report progress every 10%
                    int pct = (int)((i + 1.0) / total * 100);
                    if (pct % 10 == 0)
                    {
                        progressCallback(pct);
                    }
                }

                // Chèn chữ ký Kế toán, Người lập biểu, Hiệu trưởng ở Footer
                int endRowIdx = total + 6;
                var cell1 = worksheet.Cell(endRowIdx, 2);
                cell1.Value = "Người lập biểu\n(Ký, ghi rõ họ tên)";
                cell1.Style.Font.SetItalic(true);
                cell1.Style.Font.SetBold(true);
                cell1.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                cell1.Style.Alignment.SetWrapText(true);

                var cell2 = worksheet.Cell(endRowIdx, 5);
                cell2.Value = "Kế toán trưởng\n(Ký, ghi rõ họ tên)";
                cell2.Style.Font.SetItalic(true);
                cell2.Style.Font.SetBold(true);
                cell2.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                cell2.Style.Alignment.SetWrapText(true);

                var cell3 = worksheet.Cell(endRowIdx, 8);
                cell3.Value = "Hiệu trưởng duyệt\n(Ký tên, đóng dấu)";
                cell3.Style.Font.SetItalic(true);
                cell3.Style.Font.SetBold(true);
                cell3.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                cell3.Style.Alignment.SetWrapText(true);

                worksheet.Row(endRowIdx).Height = 45;

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(filePath);
                progressCallback(100);
            });
        }

        private static void SetCellValue(IXLCell cell, object? val)
        {
            if (val == null)
            {
                cell.Value = string.Empty;
            }
            else if (val is decimal dec)
            {
                cell.Value = (double)dec;
            }
            else if (val is int intVal)
            {
                cell.Value = intVal;
            }
            else if (val is double dbVal)
            {
                cell.Value = dbVal;
            }
            else if (val is DateTime dtVal)
            {
                cell.Value = dtVal;
            }
            else
            {
                cell.Value = val.ToString() ?? string.Empty;
            }
        }
    }
}
