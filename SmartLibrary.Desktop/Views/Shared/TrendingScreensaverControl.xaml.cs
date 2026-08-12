using System.Windows.Controls;
using System.Windows.Input;

namespace SmartLibrary.Desktop.Views.Shared
{
    public partial class TrendingScreensaverControl : UserControl
    {
        public TrendingScreensaverControl()
        {
            InitializeComponent();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Execute command if bound
            if (DataContext is ViewModels.KioskModeViewModel vm)
            {
                vm.ResetIdleTimer(); // Hide screensaver
            }
        }
    }
}
