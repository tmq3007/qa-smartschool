using System;
using System.Globalization;
using System.Windows.Data;

namespace QASmartTouch.PeriodicTable.Converters
{
    public class PositionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int position)
            {
                // Mỗi ô có kích thước 65px với khoảng cách 2px
                return (position - 1) * 67;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RowPositionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int row)
            {
                // Mỗi hàng có kích thước 65px với khoảng cách 2px, không cần offset
                return (row - 1) * 67;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ColumnPositionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int column)
            {
                // Mỗi cột có kích thước 65px với khoảng cách 2px, không cần offset
                return (column - 1) * 67;
            }
            return 75;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorString)
            {
                try
                {
                    return new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorString));
                }
                catch
                {
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray);
                }
            }
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class HighlightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isHighlighted && isHighlighted)
            {
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Yellow);
            }
            return System.Windows.Media.Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ElementOpacityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 4 && 
                values[0] is bool isHighlighted && 
                values[1] is string selectedCategory)
            {
                var selectedPeriod = values[2] as int?;
                var selectedGroup = values[3] as int?;
                
                // Nếu đang trong trạng thái tìm kiếm (SEARCH)
                if (selectedCategory == "SEARCH")
                {
                    return isHighlighted ? 1.0 : 0.3; 
                }
                
                // Nếu có bất kỳ filter nào đang active
                if (!string.IsNullOrEmpty(selectedCategory) || 
                    selectedPeriod.HasValue || 
                    selectedGroup.HasValue)
                {
                    return isHighlighted ? 1.0 : 0.3;
                }
                
                // Mặc định - không có filter nào
                return 1.0;
            }
            return 1.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ElementDetailVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2)
            {
                var selectedElement = values[0];
                var selectedCategory = values[1] as string;

                // Chỉ hiển thị khi có nguyên tố được chọn rõ ràng (không null và không phải DependencyProperty.UnsetValue)
                bool hasValidElement = selectedElement != null && selectedElement != System.Windows.DependencyProperty.UnsetValue;
                bool isCategorySelected = !string.IsNullOrEmpty(selectedCategory) && selectedCategory != "SEARCH";
                
                return (hasValidElement && !isCategorySelected) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }
            return System.Windows.Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CategoryButtonOpacityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return 1.0;
            
            string buttonCategory = values[0] as string;
            string selectedCategory = values[1] as string;

            // Nếu không có category nào được chọn, tất cả button đều opacity 1.0
            if (string.IsNullOrEmpty(selectedCategory) || selectedCategory == "SEARCH")
                return 1.0;

            // Nếu đây là button được chọn, opacity 1.0; nếu không, opacity 0.3
            return selectedCategory == buttonCategory ? 1.0 : 0.3;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class EqualityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2)
            {
                return object.Equals(values[0], values[1]);
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
