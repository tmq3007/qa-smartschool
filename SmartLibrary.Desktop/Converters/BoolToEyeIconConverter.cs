using System;
using System.Globalization;
using System.Windows.Data;

namespace SmartLibrary.Desktop.Converters
{
    public class BoolToEyeIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? "🙈" : "👁️";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IndexToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int currentIndex && parameter is string targetStr && int.TryParse(targetStr, out int targetIndex))
            {
                if (currentIndex == targetIndex)
                {
                    return System.Windows.Media.Brushes.Orange;
                }
            }
            return new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#475569"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
