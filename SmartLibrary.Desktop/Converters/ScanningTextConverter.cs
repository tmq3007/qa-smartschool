using System;
using System.Globalization;
using System.Windows.Data;

namespace SmartLibrary.Desktop.Converters
{
    public class ScanningTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isScanning)
            {
                return isScanning ? "⏹️ DỪNG QUÉT (STOP)" : "📡 BẮT ĐẦU QUÉT RFID (START)";
            }
            return "📡 BẮT ĐẦU QUÉT RFID (START)";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
