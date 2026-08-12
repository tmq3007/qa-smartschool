using System;
using System.Globalization;
using System.Windows.Data;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop.Converters;

public class XpRedeemAvailabilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is int currentXp && values[1] is XpRewardDto reward)
        {
            bool isStockOk = reward.RewardType == "Privileged" || reward.RewardType == "Border" || reward.RewardType == "Avatar" || reward.StockCount > 0;
            return currentXp >= reward.XpCost && isStockOk;
        }
        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
