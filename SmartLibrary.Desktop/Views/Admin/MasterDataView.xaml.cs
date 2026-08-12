using System.Windows.Controls;
using System.Windows.Input;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop.Views.Admin
{
    public partial class MasterDataView : UserControl
    {
        public MasterDataView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Focus();
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
            {
                if (MainTabControl != null)
                {
                    if (MainTabControl.SelectedIndex == 0)
                    {
                        TxtPubSearch.Focus();
                        TxtPubSearch.SelectAll();
                    }
                    else if (MainTabControl.SelectedIndex == 1)
                    {
                        TxtTypeSearch.Focus();
                        TxtTypeSearch.SelectAll();
                    }
                    else if (MainTabControl.SelectedIndex == 2)
                    {
                        TxtDepSearch.Focus();
                        TxtDepSearch.SelectAll();
                    }
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.F5)
            {
                if (DataContext is MasterDataViewModel vm)
                {
                    vm.Activate();
                    e.Handled = true;
                }
            }
        }
    }
}
