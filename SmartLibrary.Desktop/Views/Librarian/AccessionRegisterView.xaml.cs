using System.Windows.Controls;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop.Views.Librarian
{
    public partial class AccessionRegisterView : UserControl
    {
        public AccessionRegisterView()
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
            if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control && e.Key == System.Windows.Input.Key.F)
            {
                TxtSearch.Focus();
                TxtSearch.SelectAll();
                e.Handled = true;
            }
            else if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Alt)
            {
                if (e.SystemKey == System.Windows.Input.Key.P)
                {
                    ComboPublisher.Focus();
                    ComboPublisher.IsDropDownOpen = true;
                    e.Handled = true;
                }
                else if (e.SystemKey == System.Windows.Input.Key.S)
                {
                    ComboSource.Focus();
                    ComboSource.IsDropDownOpen = true;
                    e.Handled = true;
                }
                else if (e.SystemKey == System.Windows.Input.Key.T)
                {
                    ComboStatus.Focus();
                    ComboStatus.IsDropDownOpen = true;
                    e.Handled = true;
                }
            }
            else if (e.Key == System.Windows.Input.Key.PageUp)
            {
                if (DataContext is AccessionRegisterViewModel vm && vm.PrevPageCommand.CanExecute(null))
                {
                    vm.PrevPageCommand.Execute(null);
                    e.Handled = true;
                }
            }
            else if (e.Key == System.Windows.Input.Key.PageDown)
            {
                if (DataContext is AccessionRegisterViewModel vm && vm.NextPageCommand.CanExecute(null))
                {
                    vm.NextPageCommand.Execute(null);
                    e.Handled = true;
                }
            }
            else if (e.Key == System.Windows.Input.Key.F5)
            {
                if (DataContext is AccessionRegisterViewModel vm)
                {
                    _ = vm.LoadDataFromServerAsync();
                    e.Handled = true;
                }
            }
        }
    }
}
