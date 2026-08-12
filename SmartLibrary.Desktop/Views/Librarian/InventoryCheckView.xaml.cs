using System.Windows.Controls;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop.Views.Librarian
{
    public partial class InventoryCheckView : UserControl
    {
        public InventoryCheckView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Focus();
        }

        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is InventoryCheckViewModel vm)
            {
                if (vm.IsScanning)
                {
                    vm.ToggleScanCommand.Execute(null); // Tắt quét khi rời khỏi màn hình
                }
                vm.Cleanup();
            }
        }

        private void UserControl_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (DataContext is InventoryCheckViewModel vm)
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    if (vm.HasMisplacedBook)
                    {
                        vm.HasMisplacedBook = false;
                        SmartLibrary.Desktop.Services.SpeechService.Stop();
                        e.Handled = true;
                    }
                }
                else if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Alt)
                {
                    if (e.SystemKey == System.Windows.Input.Key.X)
                    {
                        TxtTargetGridX.Focus();
                        TxtTargetGridX.SelectAll();
                        e.Handled = true;
                    }
                    else if (e.SystemKey == System.Windows.Input.Key.Y)
                    {
                        TxtTargetGridY.Focus();
                        TxtTargetGridY.SelectAll();
                        e.Handled = true;
                    }
                }
                else if (e.Key == System.Windows.Input.Key.F5)
                {
                    vm.Activate();
                    e.Handled = true;
                }
            }
        }
    }
}
