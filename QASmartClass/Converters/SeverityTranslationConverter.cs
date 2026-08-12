using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace QASmartClass.Converters
{
    public class SeverityTranslationConverter : IValueConverter
    {
        private static readonly Dictionary<string, string> _translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Mild", "Nhẹ" },
            { "Moderate", "Trung bình" },
            { "Severe", "Nguy cấp/Nặng" }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string key)
            {
                if (_translations.TryGetValue(key, out string translated))
                {
                    return translated;
                }
                return key;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
