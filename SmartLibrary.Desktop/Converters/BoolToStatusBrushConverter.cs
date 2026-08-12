using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SmartLibrary.Desktop.Converters;

public class BoolToStatusBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isProcessed)
        {
            // Trả về màu Emerald Green nếu đã xử lý, màu Amber Yellow nếu chờ duyệt
            string hexColor = isProcessed ? "#10B981" : "#F59E0B";
            return (SolidColorBrush)new BrushConverter().ConvertFromString(hexColor)!;
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
