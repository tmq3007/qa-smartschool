using System.Windows.Controls;
using System.Windows.Input;

namespace SmartLibrary.Desktop.Views.Teacher
{
    public partial class TeacherDashboardView : UserControl
    {
        public TeacherDashboardView()
        {
            InitializeComponent();
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (MainTabControl.SelectedIndex == 0)
                {
                    TxtBookSearch.Focus();
                    TxtBookSearch.SelectAll();
                }
                else if (MainTabControl.SelectedIndex == 1)
                {
                    TxtStudentSearch.Focus();
                    TxtStudentSearch.SelectAll();
                }
                e.Handled = true;
            }
        }
    }
}
