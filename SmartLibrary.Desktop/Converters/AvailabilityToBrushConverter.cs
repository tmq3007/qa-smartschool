using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SmartLibrary.Desktop.Converters
{
    public class AvailabilityToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                // 0 cuốn -> Đỏ nhạt nền/Chữ đỏ. > 0 cuốn -> Xanh nhạt nền/Chữ xanh.
                if (parameter?.ToString() == "Background")
                {
                    return count == 0 
                        ? new SolidColorBrush(Color.FromArgb(0x20, 0xEF, 0x44, 0x44)) // Red-500 @ 12.5% opacity
                        : new SolidColorBrush(Color.FromArgb(0x20, 0x10, 0xB9, 0x81)); // Emerald-500 @ 12.5% opacity
                }
                
                return count == 0 
                    ? new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)) // Red-500
                    : new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)); // Emerald-500
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
