using System.Windows.Controls;
using System.Windows.Input;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop.Views.Student
{
    public partial class StudentBookBrowseView : UserControl
    {
        public StudentBookBrowseView()
        {
            InitializeComponent();
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                TxtSearch.Focus();
                TxtSearch.SelectAll();
                e.Handled = true;
            }
            else if (e.Key == Key.F5)
            {
                if (DataContext is StudentBookBrowseViewModel vm)
                {
                    _ = vm.LoadDataFromServerAsync();
                    e.Handled = true;
                }
            }
        }
    }
}
