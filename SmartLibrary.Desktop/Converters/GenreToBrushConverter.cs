using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SmartLibrary.Desktop.Converters
{
    public class GenreToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string genre = value?.ToString() ?? "";
            return genre switch
            {
                "Khoa học" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ECFDF5")), // Emerald-50
                "Lịch sử" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFBEB")), // Amber-50
                "Văn học" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDF2F8")), // Pink-50
                "Thiếu nhi" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFF6FF")), // Blue-50
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")) // Default Slate-50
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
