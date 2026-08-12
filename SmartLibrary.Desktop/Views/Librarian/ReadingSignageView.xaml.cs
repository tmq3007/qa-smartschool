using System.Windows.Controls;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop.Views.Librarian
{
    public partial class ReadingSignageView : UserControl
    {
        public ReadingSignageView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Focus();
        }

        private void UserControl_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (DataContext is ReadingSignageViewModel vm)
            {
                if (e.Key == System.Windows.Input.Key.Left)
                {
                    int prev = (vm.CurrentSlideIndex - 1 + 3) % 3;
                    if (vm.ChangeSlideCommand.CanExecute(prev))
                    {
                        vm.ChangeSlideCommand.Execute(prev);
                        e.Handled = true;
                    }
                }
                else if (e.Key == System.Windows.Input.Key.Right || e.Key == System.Windows.Input.Key.Space)
                {
                    int next = (vm.CurrentSlideIndex + 1) % 3;
                    if (vm.ChangeSlideCommand.CanExecute(next))
                    {
                        vm.ChangeSlideCommand.Execute(next);
                        e.Handled = true;
                    }
                }
            }
        }

        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ReadingSignageViewModel vm)
            {
                vm.Cleanup();
            }
        }

        private void ToggleTheme_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SmartLibrary.Desktop.Services.ThemeService.ToggleTheme();
        }
    }
}
