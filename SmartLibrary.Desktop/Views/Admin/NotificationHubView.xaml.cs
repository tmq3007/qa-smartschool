using System.Windows.Controls;
using System.Windows.Input;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop.Views.Admin
{
    public partial class NotificationHubView : UserControl
    {
        public NotificationHubView()
        {
            InitializeComponent();
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                if (DataContext is NotificationHubViewModel vm && vm.LoadOverdueStudentsCommand.CanExecute(null))
                {
                    vm.LoadOverdueStudentsCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }
}
