using System.Windows.Controls;
using System.Windows.Input;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop.Views.Admin
{
    public partial class LossReportView : UserControl
    {
        public LossReportView()
        {
            InitializeComponent();
        }

        private async void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                if (DataContext is LossReportViewModel vm)
                {
                    await vm.LoadLostRecordsAsync();
                    e.Handled = true;
                }
            }
        }
    }
}
