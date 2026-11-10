using System;
using System.Globalization;
using System.Windows.Data;

namespace QASmartTouch.PeriodicTable.Converters
{
    public class MultiplierToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double num = 0;
            if (value is double d) num = d;
            else if (value is float f) num = f;
            else if (value is int i) num = i;
            else if (value != null && double.TryParse(value.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsed)) num = parsed;

            double multiplier = 40; // default
            if (parameter != null && double.TryParse(parameter.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var p)) multiplier = p;

            // Ensure non-negative
            if (num < 0) num = 0;

            // Return width in pixels
            return num * multiplier;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

