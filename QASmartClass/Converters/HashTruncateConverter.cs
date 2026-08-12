using System;
using System.Globalization;
using System.Windows.Data;

namespace QASmartClass.Converters
{
    public class HashTruncateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hash && hash.Length > 8)
            {
                return hash.Substring(0, 8) + "...";
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
