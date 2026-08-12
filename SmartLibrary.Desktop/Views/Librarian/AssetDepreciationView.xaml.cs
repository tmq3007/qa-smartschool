using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop.Views.Librarian
{
    public partial class AssetDepreciationView : UserControl
    {
        public AssetDepreciationView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is AssetDepreciationViewModel vm)
            {
                if (Keyboard.Modifiers == ModifierKeys.Alt)
                {
                    if (e.SystemKey == Key.Y)
                    {
                        ComboYear.Focus();
                        ComboYear.IsDropDownOpen = true;
                        e.Handled = true;
                    }
                }
                else if (e.Key == Key.F5)
                {
                    if (vm.CalculateCommand.CanExecute(null))
                    {
                        vm.CalculateCommand.Execute(null);
                        e.Handled = true;
                    }
                }
            }
        }
    }
}
