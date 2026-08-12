using System.Windows.Controls;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop.Views.Librarian
{
    public partial class BookManagementView : UserControl
    {
        public BookManagementView()
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Focus();
            TxtSearch.Focus(); // Bổ sung Auto-focus
            if (DataContext is BookManagementViewModel vm)
            {
                // Chỉ load nếu chưa có data để tránh load lại liên tục
                if (vm.Books.Count == 0)
                {
                    await vm.LoadBooksAsync();
                }
            }
        }

        private void TopBarControl_OnActionClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is BookManagementViewModel vm)
            {
                if (vm.AddBookCommand.CanExecute(null))
                {
                    vm.AddBookCommand.Execute(null);
                }
            }
        }

        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is BookManagementViewModel vm)
            {
                vm.Cleanup();
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
            else if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Alt)
            {
                if (e.SystemKey == System.Windows.Input.Key.C)
                {
                    ComboCategory.Focus();
                    ComboCategory.IsDropDownOpen = true;
                    e.Handled = true;
                }
                else if (e.SystemKey == System.Windows.Input.Key.A)
                {
                    ComboAgeGroup.Focus();
                    ComboAgeGroup.IsDropDownOpen = true;
                    e.Handled = true;
                }
            }
            else if (e.Key == System.Windows.Input.Key.F5)
            {
                if (DataContext is BookManagementViewModel vm && vm.LoadBooksCommand.CanExecute(null))
                {
                    vm.LoadBooksCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }
}
