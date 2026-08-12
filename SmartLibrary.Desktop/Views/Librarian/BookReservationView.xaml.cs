using System.Windows.Controls;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop.Views.Librarian
{
    public partial class BookReservationView : UserControl
    {
        public BookReservationView()
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Focus();
            TxtSearch.Focus();
            if (DataContext is BookReservationViewModel vm)
            {
                if (vm.Reservations.Count == 0)
                {
                    await vm.LoadReservationsAsync();
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
                if (DataContext is BookReservationViewModel vm && vm.LoadReservationsCommand.CanExecute(null))
                {
                    vm.LoadReservationsCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        private void DataGridRow_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && row.Item is BookReservationItem item)
            {
                if (DataContext is BookReservationViewModel vm && vm.CheckoutReservedCommand.CanExecute(item))
                {
                    vm.CheckoutReservedCommand.Execute(item);
                    e.Handled = true;
                }
            }
        }
    }
}
