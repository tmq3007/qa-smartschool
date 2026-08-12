using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace QASmartClass.Converters
{
    public class IncidentTypeConverter : IValueConverter
    {
        private static readonly Dictionary<string, string> _translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Injury", "Chấn thương / Tai nạn" },
            { "Illness", "Ốm đột xuất / Mệt mỏi" },
            { "Poisoning", "Nghi ngờ ngộ độc thực phẩm" }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string key && _translations.TryGetValue(key, out string translated))
            {
                return translated;
            }
            return value ?? "Khác";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class EmergencyStatusConverter : IValueConverter
    {
        private static readonly Dictionary<string, string> _translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "RequiresAttention", "Chờ xử lý" },
            { "Resolved", "Đã sơ cứu xong" },
            { "Hospitalized", "Đã chuyển viện" },
            { "ParentsNotified", "Đã báo phụ huynh" }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string key && _translations.TryGetValue(key, out string translated))
            {
                return translated;
            }
            return value ?? "Chờ xử lý";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class EpidemicStatusConverter : IValueConverter
    {
        private static readonly Dictionary<string, string> _translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Active", "Đang điều trị" },
            { "Recovered", "Đã khỏi bệnh" }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string key && _translations.TryGetValue(key, out string translated))
            {
                return translated;
            }
            return value ?? "Đang điều trị";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class IsolationLocationConverter : IValueConverter
    {
        private static readonly Dictionary<string, string> _translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Home", "Cách ly tại nhà" },
            { "Hospital", "Cách ly tại bệnh viện" }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string key && _translations.TryGetValue(key, out string translated))
            {
                return translated;
            }
            return value ?? "Cách ly tại nhà";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class FoodSafetyResultConverter : IValueConverter
    {
        private static readonly Dictionary<string, string> _translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Pass", "Đạt chuẩn VSATTP" },
            { "Fail", "Không đạt chuẩn" }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string key && _translations.TryGetValue(key, out string translated))
            {
                return translated;
            }
            return value ?? "Chưa đánh giá";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
