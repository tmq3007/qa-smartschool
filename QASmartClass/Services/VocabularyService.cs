using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace QASmartTouch.Services
{
    /// <summary>
    /// Service tra cứu từ vựng và thuật ngữ ngoại tuyến 100% Offline
    /// Hỗ trợ giảng dạy Tiếng Anh, Toán, Lý, Hóa, STEM khi trường học ngắt mạng Internet.
    /// </summary>
    public class VocabularyService
    {
        private static Dictionary<string, string>? _offlineDict;

        public static string LookupOffline(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            EnsureDictionaryLoaded();

            string key = text.Trim().ToLowerInvariant();
            if (_offlineDict != null && _offlineDict.TryGetValue(key, out string? meaning))
            {
                return meaning;
            }

            return $"[Từ điển Offline]: Không tìm thấy từ \"{text}\" trong cơ sở dữ liệu nội bộ.";
        }

        private static void EnsureDictionaryLoaded()
        {
            if (_offlineDict != null) return;

            try
            {
                _offlineDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    // Thuật ngữ Tiếng Anh - Tiếng Việt môn Học & STEM cài sẵn
                    { "one", "Số 1" },
                    { "two", "Số 2" },
                    { "three", "Số 3" },
                    { "four", "Số 4" },
                    { "five", "Số 5" },
                    { "six", "Số 6" },
                    { "seven", "Số 7" },
                    { "eight", "Số 8" },
                    { "nine", "Số 9" },
                    { "ten", "Số 10" },
                    { "1", "Số 1" },
                    { "2", "Số 2" },
                    { "3", "Số 3" },
                    { "4", "Số 4" },
                    { "5", "Số 5" },
                    { "circle", "Hình tròn" },
                    { "square", "Hình vuông" },
                    { "triangle", "Hình tam giác" },
                    { "rectangle", "Hình chữ nhật" },
                    { "pentagon", "Hình ngũ giác" },
                    { "hexagon", "Hình lục giác" },
                    { "cube", "Hình lập phương" },
                    { "sphere", "Hình cầu" },
                    { "cylinder", "Hình trụ" },
                    { "cone", "Hình nón" },
                    { "hypotenuse", "Cạnh huyền" },
                    { "bisector", "Đường phân giác" },
                    { "velocity", "Vận tốc" },
                    { "acceleration", "Gia tốc" },
                    { "force", "Lực" },
                    { "energy", "Năng lượng" },
                    { "atom", "Nguyên tử" },
                    { "molecule", "Phân tử" },
                    { "cell", "Tế bào" },
                    { "equation", "Phương trình" },
                    { "function", "Hàm số" },
                    { "angle", "Góc" },
                    { "radius", "Bán kính" },
                    { "diameter", "Đường kính" },
                    { "perimeter", "Chu vi" },
                    { "area", "Diện tích" },
                    { "volume", "Thể tích" }
                };

                // Nếu có tệp JSON bên ngoài thì nạp bổ sung
                string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Data", "Language", "VocabularyData.json");
                if (File.Exists(dbPath))
                {
                    string json = File.ReadAllText(dbPath);
                    var rawDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    if (rawDict != null)
                    {
                        foreach (var kvp in rawDict)
                        {
                            _offlineDict[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Failed to load offline dictionary: {ex.Message}");
            }
        }
    }
}
