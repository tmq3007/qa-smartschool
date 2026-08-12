using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace QASmartTouch.PeriodicTable.Converters
{
    public class StabilityColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isStable)
            {
                return isStable 
                    ? new SolidColorBrush(Color.FromRgb(76, 175, 80))   // Green (#4CAF50)
                    : new SolidColorBrush(Color.FromRgb(244, 67, 54));  // Red (#F44336)
            }

            return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gray default
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

