using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace QASmartClass.Helpers
{
    public static class CsvHelper
    {
        public static List<string[]> ParseFile(string filePath, bool hasHeader = true, char delimiter = ',')
        {
            var results = new List<string[]>();
            if (!File.Exists(filePath)) return results;

            using (var reader = new StreamReader(filePath, Encoding.UTF8, true))
            {
                var row = new List<string>();
                var currentField = new StringBuilder();
                bool inQuotes = false;
                int nextChar;
                bool isFirstChar = true;

                while ((nextChar = reader.Read()) != -1)
                {
                    char c = (char)nextChar;

                    // Strip UTF-8 BOM if present at the very beginning
                    if (isFirstChar)
                    {
                        isFirstChar = false;
                        if (c == '\uFEFF')
                        {
                            continue;
                        }
                    }

                    if (inQuotes)
                    {
                        if (c == '"')
                        {
                            int peek = reader.Peek();
                            if (peek == '"')
                            {
                                currentField.Append('"');
                                reader.Read(); // Consume escaped quote
                            }
                            else
                            {
                                inQuotes = false;
                            }
                        }
                        else
                        {
                            currentField.Append(c);
                        }
                    }
                    else
                    {
                        if (c == '"')
                        {
                            inQuotes = true;
                        }
                        else if (c == delimiter)
                        {
                            row.Add(currentField.ToString());
                            currentField.Clear();
                        }
                        else if (c == '\r')
                        {
                            if (reader.Peek() == '\n')
                            {
                                reader.Read();
                            }
                            row.Add(currentField.ToString());
                            currentField.Clear();
                            if (row.Count > 1 || (row.Count == 1 && !string.IsNullOrWhiteSpace(row[0])))
                            {
                                results.Add(row.ToArray());
                            }
                            row = new List<string>();
                        }
                        else if (c == '\n')
                        {
                            row.Add(currentField.ToString());
                            currentField.Clear();
                            if (row.Count > 1 || (row.Count == 1 && !string.IsNullOrWhiteSpace(row[0])))
                            {
                                results.Add(row.ToArray());
                            }
                            row = new List<string>();
                        }
                        else
                        {
                            currentField.Append(c);
                        }
                    }
                }

                if (row.Count > 0 || currentField.Length > 0)
                {
                    row.Add(currentField.ToString());
                    if (row.Count > 1 || (row.Count == 1 && !string.IsNullOrWhiteSpace(row[0])))
                    {
                        results.Add(row.ToArray());
                    }
                }
            }

            if (hasHeader && results.Count > 0)
            {
                results.RemoveAt(0);
            }

            return results;
        }

        public static string[] ParseLine(string line, char delimiter = ',')
        {
            if (string.IsNullOrEmpty(line)) return Array.Empty<string>();

            // Remove leading BOM if present
            if (line.StartsWith("\uFEFF", StringComparison.Ordinal))
            {
                line = line.Substring(1);
            }

            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == delimiter)
                    {
                        result.Add(current.ToString());
                        current.Clear();
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
            }

            result.Add(current.ToString());
            return result.ToArray();
        }

        public static void WriteFile(string filePath, List<string[]> rows, string[] headers)
        {
            using (var writer = new StreamWriter(filePath, false, new UTF8Encoding(true)))
            {
                if (headers != null && headers.Length > 0)
                {
                    writer.WriteLine(FormatLine(headers));
                }

                foreach (var row in rows)
                {
                    writer.WriteLine(FormatLine(row));
                }
            }
        }

        private static string FormatLine(string[] fields)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0) builder.Append(',');

                string field = fields[i] ?? "";
                if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
                {
                    builder.Append('"').Append(field.Replace("\"", "\"\"")).Append('"');
                }
                else
                {
                    builder.Append(field);
                }
            }
            return builder.ToString();
        }
    }
}
