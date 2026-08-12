using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Controls;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop.Views.Librarian
{
    public partial class ShelfHeatmapView : UserControl
    {
        public ShelfHeatmapView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
        }

        private void UserControl_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Alt && e.SystemKey == System.Windows.Input.Key.K)
            {
                ComboShelf.Focus();
                ComboShelf.IsDropDownOpen = true;
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.F5)
            {
                if (DataContext is ShelfHeatmapViewModel vm && vm.LoadHeatmapCommand.CanExecute(null))
                {
                    vm.LoadHeatmapCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }

    public class ResourceKeyToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string resourceKey)
            {
                var resource = Application.Current.TryFindResource(resourceKey);
                if (resource is Brush brush)
                {
                    return brush;
                }
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
