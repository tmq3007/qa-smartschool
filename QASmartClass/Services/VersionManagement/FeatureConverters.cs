using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QASmartTouch.Services.VersionManagement
{
    /// <summary>
    /// Converter để kiểm tra feature enabled và chuyển đổi sang Visibility.
    /// Sử dụng trong XAML Binding.
    /// </summary>
    /// <example>
    /// <![CDATA[
    /// <Button Visibility="{Binding Source={x:Static local:FeatureManager.Instance}, 
    ///         Converter={StaticResource FeatureToVisibility}, 
    ///         ConverterParameter='shapes_2d.lines_group.arrow'}"/>
    /// ]]>
    /// </example>
    public class FeatureToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string featurePath)
            {
                bool isEnabled = FeatureManager.Instance.IsEnabled(featurePath);
                return isEnabled ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter để kiểm tra feature enabled và chuyển đổi sang Boolean.
    /// </summary>
    public class FeatureToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string featurePath)
            {
                return FeatureManager.Instance.IsEnabled(featurePath);
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter kiểm tra feature enabled, trả về Visibility.Hidden thay vì Collapsed
    /// </summary>
    public class FeatureToVisibilityHiddenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string featurePath)
            {
                bool isEnabled = FeatureManager.Instance.IsEnabled(featurePath);
                return isEnabled ? Visibility.Visible : Visibility.Hidden;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Multi-value converter để kiểm tra nhiều features
    /// Trả về Visible nếu TẤT CẢ features đều enabled
    /// </summary>
    public class AllFeaturesEnabledConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string features)
            {
                var featurePaths = features.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var path in featurePaths)
                {
                    if (!FeatureManager.Instance.IsEnabled(path.Trim()))
                    {
                        return Visibility.Collapsed;
                    }
                }
                return Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Multi-value converter để kiểm tra nhiều features
    /// Trả về Visible nếu BẤT KỲ feature nào enabled
    /// </summary>
    public class AnyFeatureEnabledConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string features)
            {
                var featurePaths = features.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var path in featurePaths)
                {
                    if (FeatureManager.Instance.IsEnabled(path.Trim()))
                    {
                        return Visibility.Visible;
                    }
                }
                return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
