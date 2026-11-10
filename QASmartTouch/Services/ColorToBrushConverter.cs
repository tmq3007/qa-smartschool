using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace QASmartClass.Services
{
    /// <summary>
    /// S2-07: Converter string hex color ? SolidColorBrush cho KPI DataGrid.
    /// S? d?ng: {Binding ColorIndicator, Converter={StaticResource ColorToBrushConverter}}
    /// </summary>
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hexColor && !string.IsNullOrEmpty(hexColor))
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(hexColor);
                    return new SolidColorBrush(color);
                }
                catch { }
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

