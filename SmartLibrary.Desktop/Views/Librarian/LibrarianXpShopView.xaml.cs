using System.Windows.Controls;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop.Views.Librarian
{
    public partial class LibrarianXpShopView : UserControl
    {
        public LibrarianXpShopView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Focus();
            TxtSearch.Focus();
        }

        private void UserControl_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (DataContext is LibrarianXpShopViewModel vm)
            {
                if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control && e.Key == System.Windows.Input.Key.F)
                {
                    TxtSearch.Focus();
                    TxtSearch.SelectAll();
                    e.Handled = true;
                }
                else if (e.Key == System.Windows.Input.Key.F5)
                {
                    if (vm.LoadPendingCommand.CanExecute(null))
                    {
                        vm.LoadPendingCommand.Execute(null);
                        e.Handled = true;
                    }
                }
            }
        }
    }
}
