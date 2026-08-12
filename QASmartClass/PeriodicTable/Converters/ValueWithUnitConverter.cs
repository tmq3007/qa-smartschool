using System;
using System.Globalization;
using System.Windows.Data;

namespace QASmartTouch.PeriodicTable.Converters
{
    /// <summary>
    /// Converter để hiển thị giá trị kèm đơn vị (Value + Unit + ComparisonPrefix + Status)
    /// Ví dụ: 
    ///   - 660.32 + "°C" → "660.32°C"
    ///   - 187 + "pm" + "~" → "~187 pm"
    ///   - null + "°C" + null + "Unknown" → "Không rõ"
    /// </summary>
    public class ValueWithUnitConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // EXTREME DEBUG - ALWAYS SHOW SOMETHING TO VERIFY CONVERTER IS CALLED
            if (values == null || values.Length < 2)
                return "[CONVERTER CALLED - NO DATA]";

            var value = values[0];
            var unit = values[1] as string;
            
            // DEBUG LOG
            System.Diagnostics.Debug.WriteLine($"[ValueWithUnitConverter] Value={value} (Type={value?.GetType().Name}), Unit='{unit}'");
            
            // Lấy ComparisonPrefix (nếu có - từ values[2])
            string comparisonPrefix = values.Length > 2 ? values[2] as string : null;
            
            // Lấy Status (nếu có - từ values[3])
            string status = values.Length > 3 ? values[3] as string : null;
            
            // Lấy MinValue và MaxValue (nếu có - từ values[4] và values[5] cho Status="Range")
            var minValue = values.Length > 4 ? values[4] : null;
            var maxValue = values.Length > 5 ? values[5] : null;

            // ===== KIỂM TRA STATUS TRƯỚC =====
            if (status == "Unknown")
                return "Không rõ";
            
            if (status == "NotApplicable")
                return "Không áp dụng";
            
            if (status == "Estimated")
                return "Ước tính";
            
            // Nếu là dải giá trị, hiển thị min–max + unit
            if (status == "Range" && minValue != null && maxValue != null)
            {
                string format = parameter as string ?? "F2";
                if (minValue is double minD && maxValue is double maxD)
                {
                    string rangeDisplay = $"{minD.ToString(format, CultureInfo.InvariantCulture)}–{maxD.ToString(format, CultureInfo.InvariantCulture)}";
                    
                    // Thêm ComparisonPrefix (trước giá trị)
                    if (!string.IsNullOrEmpty(comparisonPrefix))
                        rangeDisplay = comparisonPrefix + rangeDisplay;
                    
                    // Thêm đơn vị (sau giá trị, với khoảng trắng)
                    if (!string.IsNullOrEmpty(unit))
                        return $"{rangeDisplay} {unit}";
                    
                    return rangeDisplay;
                }
            }

            // ===== XỬ LÝ GIÁ TRỊ BÌNH THƯỜNG =====
            // Nếu không có giá trị
            if (value == null || value == System.Windows.DependencyProperty.UnsetValue)
                return "Chưa xác định";

            // Nếu giá trị là string (có thể đã có đơn vị hoặc mô tả)
            if (value is string strValue)
            {
                if (string.IsNullOrWhiteSpace(strValue))
                    return "Chưa xác định";
                return strValue; // Trả về nguyên string nếu đã có format
            }

            // Nếu giá trị là số
            if (value is double doubleValue)
            {
                // Lấy format từ parameter nếu có (VD: "F2" cho 2 số thập phân)
                string format = parameter as string ?? "F2";
                string formattedValue = doubleValue.ToString(format, CultureInfo.InvariantCulture);
                
                // Thêm ComparisonPrefix (trước giá trị)
                if (!string.IsNullOrEmpty(comparisonPrefix))
                    formattedValue = comparisonPrefix + formattedValue;
                
                // Thêm đơn vị (sau giá trị, với khoảng trắng)
                if (!string.IsNullOrEmpty(unit))
                    return $"{formattedValue} {unit}";
                
                return formattedValue;
            }

            // Fallback
            return value.ToString();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

