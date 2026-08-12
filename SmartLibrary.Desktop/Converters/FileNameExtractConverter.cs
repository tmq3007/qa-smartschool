using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace SmartLibrary.Desktop.Converters
{
    public class FileNameExtractConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path && !string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    return Path.GetFileName(path);
                }
                catch
                {
                    return path;
                }
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
