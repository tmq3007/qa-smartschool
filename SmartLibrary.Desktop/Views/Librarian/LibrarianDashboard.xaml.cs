using System.Windows.Controls;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop.Views.Librarian
{
    public partial class LibrarianDashboard : UserControl
    {
        public LibrarianDashboard()
        {
            InitializeComponent();
        }
        
        private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is DashboardViewModel vm)
            {
                await vm.LoadDataAsync();
            }
        }

        private void TopBarControl_OnActionClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is DashboardViewModel vm)
            {
                if (vm.ExportReportCommand.CanExecute(null))
                {
                    vm.ExportReportCommand.Execute(null);
                }
            }
        }
    }
}
