using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Win32;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Serilog;

namespace QASmartClass.Services
{
    public class ExcelDataService
    {
        public static string GetSaveFilePath(string defaultFileName, string filter = "Excel Files (*.xlsx)|*.xlsx")
        {
            var dialog = new SaveFileDialog
            {
                FileName = defaultFileName,
                Filter = filter,
                DefaultExt = ".xlsx",
                Title = "Ch?n noi luu file"
            };

            return dialog.ShowDialog() == true ? dialog.FileName : string.Empty;
        }

        public static string GetOpenFilePath(string filter = "Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv")
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter,
                Title = "Chọn file dữ liệu"
            };

            return dialog.ShowDialog() == true ? dialog.FileName : string.Empty;
        }

        public static bool ExportToExcel<T>(string filePath, IEnumerable<T> data, string sheetName = "Sheet1", Action<ExcelWorksheet> customFormatting = null)
        {
            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add(sheetName);

                // Add data using LoadFromCollection
                var dataList = data.ToList();
                if (dataList.Any())
                {
                    worksheet.Cells["A1"].LoadFromCollection(dataList, PrintHeaders: true);
                }
                else
                {
                    // If no data, still print headers using reflection
                    var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    for (int i = 0; i < properties.Length; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = properties[i].Name;
                    }
                }

                // Format Header
                int colCount = worksheet.Dimension?.Columns ?? typeof(T).GetProperties().Length;
                if (colCount > 0)
                {
                    using var headerRange = worksheet.Cells[1, 1, 1, colCount];
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.View.FreezePanes(2, 1); // Freeze header
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                }

                // Apply any custom formatting
                customFormatting?.Invoke(worksheet);

                package.SaveAs(new FileInfo(filePath));
                Log.Information("[ExcelDataService] Exported {Count} records to {FilePath}", dataList.Count, filePath);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[ExcelDataService] ExportToExcel failed for {FilePath}", filePath);
                return false;
            }
        }

        public static bool ExportDataTableToExcel(string filePath, System.Data.DataTable table, string sheetName = "Sheet1")
        {
            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add(sheetName);

                if (table != null && table.Columns.Count > 0)
                {
                    worksheet.Cells["A1"].LoadFromDataTable(table, true);

                    // Format Header
                    using var headerRange = worksheet.Cells[1, 1, 1, table.Columns.Count];
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.View.FreezePanes(2, 1);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                }

                package.SaveAs(new FileInfo(filePath));
                Log.Information("[ExcelDataService] Exported DataTable to {FilePath}", filePath);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[ExcelDataService] ExportDataTableToExcel failed for {FilePath}", filePath);
                return false;
            }
        }

        public static System.Data.DataTable ReadExcelToDataTable(string filePath)
        {
            var table = new System.Data.DataTable();
            try
            {
                using var package = new ExcelPackage(new FileInfo(filePath));
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null || worksheet.Dimension == null)
                {
                    Log.Warning("[ExcelDataService] Excel file {FilePath} is empty or has no worksheet.", filePath);
                    return table;
                }

                int rowCount = worksheet.Dimension.Rows;
                int colCount = worksheet.Dimension.Columns;

                // Read Headers
                for (int col = 1; col <= colCount; col++)
                {
                    string colName = worksheet.Cells[1, col].Value?.ToString()?.Trim() ?? $"Column{col}";
                    // Ensure column name is unique in DataTable
                    string uniqueColName = colName;
                    int suffix = 1;
                    while (table.Columns.Contains(uniqueColName))
                    {
                        uniqueColName = $"{colName}_{suffix++}";
                    }
                    table.Columns.Add(uniqueColName);
                }

                // Read Data Rows
                for (int row = 2; row <= rowCount; row++)
                {
                    // Check if the entire row is empty to skip it
                    bool isRowEmpty = true;
                    var newRow = table.NewRow();
                    for (int col = 1; col <= colCount; col++)
                    {
                        var cellValue = worksheet.Cells[row, col].Value;
                        if (cellValue != null && !string.IsNullOrWhiteSpace(cellValue.ToString()))
                        {
                            isRowEmpty = false;
                        }
                        newRow[col - 1] = cellValue?.ToString()?.Trim() ?? string.Empty;
                    }

                    if (!isRowEmpty)
                    {
                        table.Rows.Add(newRow);
                    }
                }

                Log.Information("[ExcelDataService] Successfully read {Count} rows from {FilePath}", table.Rows.Count, filePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[ExcelDataService] ReadExcelToDataTable failed for {FilePath}", filePath);
                throw;
            }
            return table;
        }
    }
}

