using System.Windows.Controls;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop.Views.Librarian
{
    public partial class GreenEnergyView : UserControl
    {
        public GreenEnergyView(GreenEnergyViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Unloaded += GreenEnergyView_Unloaded;
        }

        private void GreenEnergyView_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is GreenEnergyViewModel vm)
            {
                vm.Cleanup();
            }
        }
    }
}
