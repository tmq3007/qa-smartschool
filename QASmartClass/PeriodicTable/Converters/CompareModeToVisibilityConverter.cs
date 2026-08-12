using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QASmartTouch.PeriodicTable.Converters
{
    public class CompareModeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int compareMode)
            {
                // Show 3rd column only when compareMode == 3
                return compareMode == 3 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class IntToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int compareMode && parameter is string targetMode)
            {
                return compareMode.ToString() == targetMode;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked && parameter is string targetMode)
            {
                return int.Parse(targetMode);
            }
            return 2; // Default
        }
    }
}

