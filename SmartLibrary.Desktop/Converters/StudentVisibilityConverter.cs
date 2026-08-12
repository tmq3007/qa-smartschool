using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SmartLibrary.Desktop.Converters
{
    public class StudentVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string role = value as string;
            // Chỉ hiện Gamification cho Học sinh và Giáo viên
            return (role == "Student" || role == "Teacher") ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
