using System.Windows.Controls;
using System.Windows.Input;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop.Views.Admin
{
    public partial class GreenMetricsView : UserControl
    {
        public GreenMetricsView()
        {
            InitializeComponent();
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                if (DataContext is GreenMetricsViewModel vm && vm.LoadMetricsCommand.CanExecute(null))
                {
                    vm.LoadMetricsCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }
}
