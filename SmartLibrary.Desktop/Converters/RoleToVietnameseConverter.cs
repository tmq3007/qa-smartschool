using System;
using System.Globalization;
using System.Windows.Data;

namespace SmartLibrary.Desktop.Converters
{
    public class RoleToVietnameseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string role)
            {
                return role.ToLower() switch
                {
                    "student" => "Học sinh",
                    "teacher" => "Giáo viên",
                    "librarian" => "Thủ thư",
                    "admin" => "Quản trị viên",
                    _ => role
                };
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
