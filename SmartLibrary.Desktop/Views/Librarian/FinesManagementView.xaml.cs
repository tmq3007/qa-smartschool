using System.Windows.Controls;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop.Views.Librarian
{
    public partial class FinesManagementView : UserControl
    {
        public FinesManagementView()
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Focus();
            TxtSearch.Focus();
            if (DataContext is FinesManagementViewModel vm)
            {
                if (vm.Fines.Count == 0)
                {
                    await vm.LoadFinesAsync();
                }
            }
        }

        private void UserControl_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control && e.Key == System.Windows.Input.Key.F)
            {
                TxtSearch.Focus();
                TxtSearch.SelectAll();
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.F5)
            {
                if (DataContext is FinesManagementViewModel vm && vm.LoadFinesCommand.CanExecute(null))
                {
                    vm.LoadFinesCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        private void DataGridRow_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && row.Item is UnpaidFineItem item)
            {
                if (DataContext is FinesManagementViewModel vm && vm.PayFineCommand.CanExecute(item))
                {
                    vm.PayFineCommand.Execute(item);
                }
            }
        }
    }
}
