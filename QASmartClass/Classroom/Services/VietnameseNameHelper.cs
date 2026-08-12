using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace QASmartClass.Classroom.Services
{
    /// <summary>
    /// Sắp xếp họ tên theo chuẩn Việt Nam:
    ///   1) Tên (từ cuối cùng)  →  2) Họ (từ đầu tiên)  →  3) Tên đệm
    /// Thứ tự bảng chữ cái tiếng Việt:
    ///   A Ă Â B C D Đ E Ê G H I K L M N O Ô Ơ P Q R S T U Ư V X Y
    /// </summary>
    public static class VietnameseNameHelper
    {
        /// <summary>
        /// Tách họ tên thành (Họ, Tên đệm, Tên).
        /// "Nguyễn Văn An" → ("Nguyễn", "Văn", "An")
        /// </summary>
        public static (string Ho, string Dem, string Ten) SplitName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return ("", "", "");

            var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return ("", "", parts[0]);
            if (parts.Length == 2) return (parts[0], "", parts[1]);

            string ho = parts[0];
            string ten = parts[^1];
            string dem = string.Join(" ", parts[1..^1]);
            return (ho, dem, ten);
        }

        /// <summary>
        /// Key sắp xếp theo chuẩn VN: Tên → Họ → Đệm (sử dụng cho OrderBy)
        /// Dùng ToVietnameseSortKey để đảm bảo đúng thứ tự bảng chữ cái VN.
        /// </summary>
        public static string SortKey(string fullName)
        {
            var (ho, dem, ten) = SplitName(fullName);
            return $"{ToVietnameseSortKey(ten)}|{ToVietnameseSortKey(ho)}|{ToVietnameseSortKey(dem)}";
        }

        /// <summary>
        /// Sắp xếp danh sách theo chuẩn VN (Tên → Họ → Đệm)
        /// </summary>
        public static List<T> SortByVietnameseName<T>(IEnumerable<T> items, Func<T, string> nameSelector)
        {
            return items
                .OrderBy(x => SortKey(nameSelector(x)), StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // ═══════════════════════════════════════════════════════════
        //  SORT KEY THEO BẢNG CHỮ CÁI TIẾNG VIỆT
        //  A(0) Ă(1) Â(2) B(3) C(4) D(5) Đ(6) E(7) Ê(8) G(9)
        //  H(10) I(11) K(12) L(13) M(14) N(15) O(16) Ô(17) Ơ(18)
        //  P(19) Q(20) R(21) S(22) T(23) U(24) Ư(25) V(26) X(27) Y(28)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Chuyển text thành sort key theo đúng thứ tự bảng chữ cái tiếng Việt.
        /// Mỗi ký tự được map thành 1 chuỗi duy nhất đảm bảo thứ tự sort đúng.
        /// VD: D → "D0", Đ → "D1" (D đứng trước Đ)
        /// </summary>
        private static string ToVietnameseSortKey(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var sb = new StringBuilder(text.Length * 2);
            foreach (char c in text)
            {
                sb.Append(MapVietnameseChar(c));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Map từng ký tự tiếng Việt (bao gồm cả dấu thanh) thành sort key.
        /// Bảng chữ cái VN: A Ă Â B C D Đ E Ê G H I K L M N O Ô Ơ P Q R S T U Ư V X Y
        /// </summary>
        private static string MapVietnameseChar(char c)
        {
            return c switch
            {
                // ═══ A group: A(0) < Ă(1) < Â(2) ═══
                'A' or 'À' or 'Á' or 'Ả' or 'Ã' or 'Ạ' => "A0",
                'a' or 'à' or 'á' or 'ả' or 'ã' or 'ạ' => "a0",
                'Ă' or 'Ằ' or 'Ắ' or 'Ẳ' or 'Ẵ' or 'Ặ' => "A1",
                'ă' or 'ằ' or 'ắ' or 'ẳ' or 'ẵ' or 'ặ' => "a1",
                'Â' or 'Ầ' or 'Ấ' or 'Ẩ' or 'Ẫ' or 'Ậ' => "A2",
                'â' or 'ầ' or 'ấ' or 'ẩ' or 'ẫ' or 'ậ' => "a2",

                // ═══ D group: D(0) < Đ(1) ═══
                'D' => "D0", 'd' => "d0",
                'Đ' => "D1", 'đ' => "d1",

                // ═══ E group: E(0) < Ê(1) ═══
                'E' or 'È' or 'É' or 'Ẻ' or 'Ẽ' or 'Ẹ' => "E0",
                'e' or 'è' or 'é' or 'ẻ' or 'ẽ' or 'ẹ' => "e0",
                'Ê' or 'Ề' or 'Ế' or 'Ể' or 'Ễ' or 'Ệ' => "E1",
                'ê' or 'ề' or 'ế' or 'ể' or 'ễ' or 'ệ' => "e1",

                // ═══ I group (chỉ có I, không có biến thể chữ cái) ═══
                'I' or 'Ì' or 'Í' or 'Ỉ' or 'Ĩ' or 'Ị' => "I",
                'i' or 'ì' or 'í' or 'ỉ' or 'ĩ' or 'ị' => "i",

                // ═══ O group: O(0) < Ô(1) < Ơ(2) ═══
                'O' or 'Ò' or 'Ó' or 'Ỏ' or 'Õ' or 'Ọ' => "O0",
                'o' or 'ò' or 'ó' or 'ỏ' or 'õ' or 'ọ' => "o0",
                'Ô' or 'Ồ' or 'Ố' or 'Ổ' or 'Ỗ' or 'Ộ' => "O1",
                'ô' or 'ồ' or 'ố' or 'ổ' or 'ỗ' or 'ộ' => "o1",
                'Ơ' or 'Ờ' or 'Ớ' or 'Ở' or 'Ỡ' or 'Ợ' => "O2",
                'ơ' or 'ờ' or 'ớ' or 'ở' or 'ỡ' or 'ợ' => "o2",

                // ═══ U group: U(0) < Ư(1) ═══
                'U' or 'Ù' or 'Ú' or 'Ủ' or 'Ũ' or 'Ụ' => "U0",
                'u' or 'ù' or 'ú' or 'ủ' or 'ũ' or 'ụ' => "u0",
                'Ư' or 'Ừ' or 'Ứ' or 'Ử' or 'Ữ' or 'Ự' => "U1",
                'ư' or 'ừ' or 'ứ' or 'ử' or 'ữ' or 'ự' => "u1",

                // ═══ Y group (chỉ có Y) ═══
                'Y' or 'Ỳ' or 'Ý' or 'Ỷ' or 'Ỹ' or 'Ỵ' => "Y",
                'y' or 'ỳ' or 'ý' or 'ỷ' or 'ỹ' or 'ỵ' => "y",

                // ═══ Các ký tự khác giữ nguyên ═══
                _ => c.ToString()
            };
        }

        /// <summary>
        /// Xóa dấu tiếng Việt (dùng cho tìm kiếm, KHÔNG dùng cho sắp xếp).
        /// "Ắ" → "A", "Đ" → "D", "Ô" → "O"
        /// </summary>
        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            text = text.Replace('Đ', 'D').Replace('đ', 'd');
            var normalized = text.Normalize(NormalizationForm.FormD);
            var chars = normalized
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray();
            return new string(chars).Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Lấy Tên (từ cuối cùng) từ họ tên đầy đủ
        /// </summary>
        public static string GetFirstName(string fullName)
        {
            return SplitName(fullName).Ten;
        }
    }
}
