using System.Windows.Controls;
using System.Windows.Input;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop.Views.Admin
{
    public partial class SystemLogView : UserControl
    {
        public SystemLogView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Focus();
        }

        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is SystemLogViewModel vm)
            {
                vm.Cleanup();
            }
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
            {
                TxtSearch.Focus();
                TxtSearch.SelectAll();
                e.Handled = true;
            }
            else if (e.Key == Key.F5)
            {
                if (DataContext is SystemLogViewModel vm && vm.SyncPendingCommand.CanExecute(null))
                {
                    vm.SyncPendingCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }
}
