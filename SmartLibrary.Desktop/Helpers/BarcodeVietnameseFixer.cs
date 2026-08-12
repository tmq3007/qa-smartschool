using System.Collections.Generic;

namespace SmartLibrary.Desktop.Helpers
{
    public static class BarcodeVietnameseFixer
    {
        private static readonly Dictionary<string, string> TelexMap = new()
        {
            { "â", "aa" }, { "Â", "AA" },
            { "ă", "aw" }, { "Ă", "AW" },
            { "đ", "dd" }, { "Đ", "DD" },
            { "ê", "ee" }, { "Ê", "EE" },
            { "ô", "oo" }, { "Ô", "OO" },
            { "ơ", "ow" }, { "Ơ", "OW" },
            { "ư", "uw" }, { "Ư", "UW" },
            
            { "á", "as" }, { "Á", "AS" },
            { "à", "af" }, { "À", "AF" },
            { "ả", "ar" }, { "Ả", "AR" },
            { "ã", "ax" }, { "Ã", "AX" },
            { "ạ", "aj" }, { "Ạ", "AJ" },
            
            { "ấ", "aas" }, { "Ấ", "AAS" },
            { "ầ", "aaf" }, { "Ầ", "AAF" },
            { "ẩ", "aar" }, { "Ẩ", "AAR" },
            { "ẫ", "aax" }, { "Ẫ", "AAX" },
            { "ậ", "aaj" }, { "Ậ", "AAJ" },
            
            { "ắ", "aws" }, { "Ắ", "AWS" },
            { "ằ", "awf" }, { "Ằ", "AWF" },
            { "ẳ", "awr" }, { "Ẳ", "AWR" },
            { "ẵ", "awx" }, { "Ẵ", "AWX" },
            { "ặ", "awj" }, { "Ặ", "AWJ" },
            
            { "é", "es" }, { "É", "ES" },
            { "è", "ef" }, { "È", "EF" },
            { "ẻ", "er" }, { "Ẻ", "ER" },
            { "ẽ", "ex" }, { "Ẽ", "EX" },
            { "ẹ", "ej" }, { "Ẹ", "EJ" },
            
            { "ế", "ees" }, { "Ế", "EES" },
            { "ề", "eef" }, { "Ề", "EEF" },
            { "ể", "eer" }, { "Ể", "EER" },
            { "ễ", "eex" }, { "Ễ", "EEX" },
            { "ệ", "eej" }, { "Ệ", "EEJ" },
            
            { "í", "is" }, { "Í", "IS" },
            { "ì", "if" }, { "Ì", "IF" },
            { "ỉ", "ir" }, { "Ỉ", "IR" },
            { "ĩ", "ix" }, { "Ĩ", "IX" },
            { "ị", "ij" }, { "Ị", "IJ" },
            
            { "ó", "os" }, { "Ó", "OS" },
            { "ò", "of" }, { "Ò", "OF" },
            { "ỏ", "or" }, { "Ỏ", "OR" },
            { "õ", "ox" }, { "Õ", "OX" },
            { "ọ", "oj" }, { "Ọ", "OJ" },
            
            { "ố", "oos" }, { "Ố", "OOS" },
            { "ồ", "oof" }, { "Ồ", "OOF" },
            { "ổ", "oor" }, { "Ổ", "OOR" },
            { "ỗ", "oox" }, { "Ỗ", "OOX" },
            { "ộ", "ooj" }, { "Ộ", "OOJ" },
            
            { "ớ", "ows" }, { "Ớ", "OWS" },
            { "ờ", "owf" }, { "Ờ", "OWF" },
            { "ở", "owr" }, { "Ở", "OWR" },
            { "ỡ", "owx" }, { "Ỡ", "OWX" },
            { "ợ", "owj" }, { "Ợ", "OWJ" },
            
            { "ú", "us" }, { "Ú", "US" },
            { "ù", "uf" }, { "Ù", "UF" },
            { "ủ", "ur" }, { "Ủ", "UR" },
            { "ũ", "ux" }, { "Ũ", "UX" },
            { "ụ", "uj" }, { "Ụ", "UJ" },
            
            { "ứ", "uws" }, { "Ứ", "UWS" },
            { "ừ", "uwf" }, { "Ừ", "UWF" },
            { "ử", "uwr" }, { "Ử", "UWR" },
            { "ữ", "uwx" }, { "Ữ", "UWX" },
            { "ự", "uwj" }, { "Ự", "UWJ" }
        };

        private static readonly Dictionary<string, string> VniMap = new()
        {
            { "á", "a1" }, { "Á", "A1" },
            { "à", "a2" }, { "À", "A2" },
            { "ả", "a3" }, { "Ả", "A3" },
            { "ã", "a4" }, { "Ã", "A4" },
            { "ạ", "a5" }, { "Ạ", "A5" },
            { "â", "a6" }, { "Â", "A6" },
            { "đ", "d9" }, { "Đ", "D9" },
            { "ă", "a8" }, { "Ă", "A8" },
            { "ê", "e6" }, { "Ê", "E6" },
            { "ô", "o6" }, { "Ô", "O6" },
            { "ơ", "o7" }, { "Ơ", "O7" },
            { "ư", "u7" }, { "Ư", "U7" }
        };

        /// <summary>
        /// Tự động phát hiện và dịch ngược các ký tự unicode tiếng Việt bị lỗi do bộ gõ (Telex/VNI) về mã ASCII gốc.
        /// </summary>
        public static string Fix(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            // Kiểm tra xem chuỗi có chứa ký tự unicode không
            bool hasNonAscii = false;
            foreach (char c in input)
            {
                if (c > 127)
                {
                    hasNonAscii = true;
                    break;
                }
            }

            if (!hasNonAscii) return input.Trim();

            // Thực hiện chuyển dịch Telex trước (chiếm 95% các trường hợp)
            string fixedText = input;
            foreach (var kvp in TelexMap)
            {
                fixedText = fixedText.Replace(kvp.Key, kvp.Value);
            }

            // Nếu vẫn còn ký tự unicode, thử chuyển dịch theo VNI
            hasNonAscii = false;
            foreach (char c in fixedText)
            {
                if (c > 127)
                {
                    hasNonAscii = true;
                    break;
                }
            }

            if (hasNonAscii)
            {
                foreach (var kvp in VniMap)
                {
                    fixedText = fixedText.Replace(kvp.Key, kvp.Value);
                }
            }

            return fixedText.Trim();
        }
    }
}
