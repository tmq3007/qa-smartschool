using System.Windows;
using System.Windows.Controls;

namespace SmartLibrary.Desktop.Views.Librarian
{
    public partial class AudiobookApprovalView : UserControl
    {
        public AudiobookApprovalView()
        {
            InitializeComponent();
            Unloaded += (s, e) =>
            {
                if (DataContext is ViewModels.AudiobookApprovalViewModel vm)
                {
                    vm.StopAudio();
                }
            };
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
            SearchBox.Focus();
            if (DataContext is ViewModels.AudiobookApprovalViewModel vm)
            {
                if (vm.PendingAudiobooks.Count == 0)
                {
                    await vm.LoadPendingAudiobooksAsync();
                }
            }
        }

        private void UserControl_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control && e.Key == System.Windows.Input.Key.F)
            {
                SearchBox.Focus();
                SearchBox.SelectAll();
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.F5)
            {
                if (DataContext is ViewModels.AudiobookApprovalViewModel vm && vm.LoadPendingAudiobooksCommand.CanExecute(null))
                {
                    vm.LoadPendingAudiobooksCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }
}
