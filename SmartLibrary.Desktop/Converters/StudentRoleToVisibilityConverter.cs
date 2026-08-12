using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SmartLibrary.Desktop.Converters
{
    public class StudentRoleToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is string role && role == "Student") ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
