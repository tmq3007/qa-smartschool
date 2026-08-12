using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop.Views.Librarian
{
    public partial class UsageReportsView : UserControl
    {
        public UsageReportsView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                if (DataContext is UsageReportsViewModel vm && vm.LoadStatsCommand.CanExecute(null))
                {
                    vm.LoadStatsCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }
}
