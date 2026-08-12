using System.Globalization;
using System.Text.RegularExpressions;

namespace SmartLibrary.Desktop.Helpers
{
    public static class InputHelper
    {
        /// <summary>
        /// Trim + Title Case. VD: "  nhà xuất BẢN  " → "Nhà Xuất Bản"
        /// </summary>
        public static string NormalizeInput(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            input = input.Trim();
            input = SanitizeHtml(input);
            
            var originalWords = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var viCulture = new CultureInfo("vi-VN");
            
            var titleCased = viCulture.TextInfo.ToTitleCase(input.ToLower(viCulture));
            var titleCasedWords = titleCased.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            for (int i = 0; i < originalWords.Length; i++)
            {
                if (i >= titleCasedWords.Length) break;
                
                string orig = originalWords[i];
                // Bảo tồn từ viết tắt nếu nguyên bản viết hoa toàn bộ (độ dài >= 2)
                bool isAcronym = orig.Length >= 2 && orig.All(c => char.IsUpper(c) || !char.IsLetter(c));
                if (isAcronym)
                {
                    titleCasedWords[i] = orig;
                }
            }
            
            return string.Join(" ", titleCasedWords);
        }

        /// <summary>
        /// Chỉ Trim, không Title Case. Dùng cho Description, Address.
        /// </summary>
        public static string TrimInput(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            return SanitizeHtml(input.Trim());
        }

        /// <summary>
        /// Loại bỏ ký tự HTML nguy hiểm
        /// </summary>
        public static string SanitizeHtml(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            // Xóa tag HTML
            input = Regex.Replace(input, @"<[^>]*>", "");
            // Xóa ký tự đặc biệt SQL injection
            input = input.Replace("'", "\u2019").Replace("\"", "\u201C");
            return input;
        }

        /// <summary>
        /// Kiểm tra độ dài hợp lệ
        /// </summary>
        public static bool ValidateLength(string? input, int min, int max)
        {
            if (string.IsNullOrWhiteSpace(input)) return min == 0;
            int len = input.Trim().Length;
            return len >= min && len <= max;
        }

        /// <summary>
        /// Kiểm định ISBN hợp lệ với thuật toán tính toán checksum của tiêu chuẩn quốc tế (ISBN-10 và ISBN-13)
        /// </summary>
        public static bool ValidateIsbn(string? isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn)) return false;
            var clean = isbn.Replace("-", "").Replace(" ", "").Trim().ToUpper();
            
            if (clean.Length == 10)
            {
                return ValidateIsbn10Checksum(clean);
            }
            else if (clean.Length == 13)
            {
                return ValidateIsbn13Checksum(clean);
            }
            return false;
        }

        private static bool ValidateIsbn10Checksum(string isbn)
        {
            if (isbn.Length != 10) return false;
            string body = isbn.Substring(0, 9);
            if (!body.All(char.IsDigit)) return false;

            char lastChar = isbn[9];
            if (!char.IsDigit(lastChar) && lastChar != 'X') return false;

            int sum = 0;
            for (int i = 0; i < 9; i++)
            {
                sum += (isbn[i] - '0') * (10 - i);
            }

            int lastVal = (lastChar == 'X') ? 10 : (lastChar - '0');
            sum += lastVal;

            return sum % 11 == 0;
        }

        private static bool ValidateIsbn13Checksum(string isbn)
        {
            if (isbn.Length != 13 || !isbn.All(char.IsDigit)) return false;

            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int digit = isbn[i] - '0';
                sum += (i % 2 == 0) ? digit : digit * 3;
            }

            int checkDigit = (10 - (sum % 10)) % 10;
            return checkDigit == (isbn[12] - '0');
        }

        /// <summary>
        /// Chuẩn hóa tên nhà xuất bản theo quy chuẩn sư phạm Việt Nam
        /// </summary>
        public static string NormalizePublisherName(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "NXB Tổng Hợp";
            string raw = RemoveDiacritics(input).ToLower().Trim();

            if (raw.Contains("giao duc") || raw.Contains("nxb gd") || raw.Equals("gd"))
                return "NXB Giáo Dục Việt Nam";
            if (raw.Contains("su pham") || raw.Contains("dhsp") || raw.Equals("sp"))
                return "NXB Đại học Sư phạm";
            if (raw.Contains("kim dong") || raw.Contains("nxb kd"))
                return "NXB Kim Đồng";
            if (raw.Contains("tre"))
                return "NXB Trẻ";

            return NormalizeInput(input); // Fallback về Title Case tiêu chuẩn
        }

        /// <summary>
        /// Loại bỏ dấu tiếng Việt của chuỗi Unicode
        /// </summary>
        public static string RemoveDiacritics(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            
            string normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
            System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();

            foreach (char c in normalizedString)
            {
                System.Globalization.UnicodeCategory unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            string result = stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC);
            result = result.Replace('đ', 'd').Replace('Đ', 'D');
            
            return result;
        }
    }
}
