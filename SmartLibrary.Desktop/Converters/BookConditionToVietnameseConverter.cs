using System;
using System.Globalization;
using System.Windows.Data;

namespace SmartLibrary.Desktop.Converters
{
    public class BookConditionToVietnameseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string cond)
            {
                return cond.ToLower() switch
                {
                    "good" => "Tốt / Bình thường",
                    "damaged" => "Hỏng nhẹ / Rách",
                    "lost" => "Thất lạc sách",
                    _ => cond
                };
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string viet)
            {
                return viet.ToLower() switch
                {
                    "tốt / bình thường" => "Good",
                    "hỏng nhẹ / rách" => "Damaged",
                    "thất lạc sách" => "Lost",
                    _ => viet
                };
            }
            return value;
        }
    }
}
