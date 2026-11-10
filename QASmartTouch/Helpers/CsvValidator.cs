using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace QASmartClass.Helpers
{
    public class CsvValidationError
    {
        public int RowIndex { get; set; }
        public string Column { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class CsvValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public bool IsUtf8 { get; set; } = true;
        public List<CsvValidationError> Errors { get; set; } = new();
        public List<string[]> Rows { get; set; } = new();
        public string[] Headers { get; set; } = Array.Empty<string>();
    }

    public static class CsvValidator
    {
        public static CsvValidationResult Validate(string filePath, string[] requiredHeaders, Dictionary<string, Func<string, bool>> columnValidators = null)
        {
            var result = new CsvValidationResult();
            if (!File.Exists(filePath))
            {
                result.Errors.Add(new CsvValidationError { RowIndex = 0, ErrorMessage = "Không tìm thấy file." });
                return result;
            }

            // Check UTF-8 encoding
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                byte[] bom = new byte[3];
                int bytesRead = fs.Read(bom, 0, 3);
                if (bytesRead >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
                {
                    result.IsUtf8 = true;
                }
                else
                {
                    result.IsUtf8 = true; // default assume true
                }
            }
            catch {}

            try
            {
                using var reader = new StreamReader(filePath, Encoding.UTF8);
                string headerLine = reader.ReadLine();
                if (string.IsNullOrEmpty(headerLine))
                {
                    result.Errors.Add(new CsvValidationError { RowIndex = 1, ErrorMessage = "File CSV rỗng." });
                    return result;
                }

                // Detect delimiter (, or ;)
                char delimiter = ',';
                if (headerLine.Contains(";") && !headerLine.Contains(","))
                {
                    delimiter = ';';
                }

                var headers = SplitCsvLine(headerLine, delimiter);
                result.Headers = headers;

                // Validate Headers
                var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < headers.Length; i++)
                {
                    headerMap[headers[i].Trim()] = i;
                }

                foreach (var req in requiredHeaders)
                {
                    if (!headerMap.ContainsKey(req))
                    {
                        result.Errors.Add(new CsvValidationError { RowIndex = 1, Column = req, ErrorMessage = $"Thiếu cột bắt buộc: '{req}'." });
                    }
                }

                if (!result.IsValid)
                {
                    return result;
                }

                string line;
                int rowIdx = 2;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var fields = SplitCsvLine(line, delimiter);
                    result.Rows.Add(fields);

                    // Validate columns in row
                    foreach (var req in requiredHeaders)
                    {
                        int colIdx = headerMap[req];
                        if (colIdx >= fields.Length)
                        {
                            result.Errors.Add(new CsvValidationError { RowIndex = rowIdx, Column = req, ErrorMessage = "Không có dữ liệu ở cột này." });
                            continue;
                        }

                        string val = fields[colIdx].Trim();
                        if (string.IsNullOrEmpty(val))
                        {
                            result.Errors.Add(new CsvValidationError { RowIndex = rowIdx, Column = req, ErrorMessage = "Dữ liệu bắt buộc không được để trống." });
                            continue;
                        }

                        if (columnValidators != null && columnValidators.TryGetValue(req, out var validator))
                        {
                            if (!validator(val))
                            {
                                result.Errors.Add(new CsvValidationError { RowIndex = rowIdx, Column = req, ErrorMessage = "Dữ liệu sai định dạng." });
                            }
                        }
                    }
                    rowIdx++;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(new CsvValidationError { RowIndex = 0, ErrorMessage = $"Lỗi đọc file: {ex.Message}" });
            }

            return result;
        }

        private static string[] SplitCsvLine(string line, char delimiter)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var currentField = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == delimiter && !inQuotes)
                {
                    result.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }
            result.Add(currentField.ToString());
            return result.ToArray();
        }
    }
}
